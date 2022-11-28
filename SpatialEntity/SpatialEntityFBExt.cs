using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using StereoKit;
using StereoKit.Framework;

using XrSpace            = System.UInt64;
using XrAsyncRequestIdFB = System.UInt64;

namespace SpatialEntity
{
	public class SpatialEntityFBExt : IStepper
	{
		bool extAvailable;
		bool enabled;

		public Dictionary<Guid, Anchor> Anchors = new Dictionary<Guid, Anchor>();

		public class Anchor
		{
			public XrSpace XrSpace;
			public Pose Pose;
			public bool LocateSuccess;
		}

		public bool Available => extAvailable;
		public bool Enabled { get => extAvailable && enabled; set => enabled = value; }

		public SpatialEntityFBExt() : this(true) { }
		public SpatialEntityFBExt(bool enabled = true)
		{
			if (SK.IsInitialized)
				Log.Err("SpatialEntityFBExt must be constructed before StereoKit is initialized!");
			Backend.OpenXR.RequestExt("XR_FB_spatial_entity");
			Backend.OpenXR.RequestExt("XR_FB_spatial_entity_storage");
			Backend.OpenXR.RequestExt("XR_FB_spatial_entity_query");
		}

		public bool Initialize()
		{
			extAvailable =
				Backend.XRType == BackendXRType.OpenXR
				&& Backend.OpenXR.ExtEnabled("XR_FB_spatial_entity")
				&& Backend.OpenXR.ExtEnabled("XR_FB_spatial_entity_storage")
				&& Backend.OpenXR.ExtEnabled("XR_FB_spatial_entity_query")
				&& LoadBindings();

			// Set up xrPollEvent subscription
			if (extAvailable)
			{
				Backend.OpenXR.OnPollEvent += pollEventHandler;
			}

			return true;
		}

		public void Step()
		{
			// Update the anchor pose periodically because the anchor may drift slightly in the Meta Quest runtime.
			foreach(var anchor in Anchors.Values)
			{
				XrSpaceLocation spaceLocation = new XrSpaceLocation { type = XrStructureType.XR_TYPE_SPACE_LOCATION };

				// TODO consider using XrFrameState.predictedDisplayTime for XrTime argument
				XrResult result = xrLocateSpace(anchor.XrSpace, Backend.OpenXR.Space, Backend.OpenXR.Time, out spaceLocation);
				if (result == XrResult.Success)
				{
					var orientationValid = spaceLocation.locationFlags.HasFlag(XrSpaceLocationFlags.XR_SPACE_LOCATION_ORIENTATION_VALID_BIT);
					var poseValid        = spaceLocation.locationFlags.HasFlag(XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_VALID_BIT);
					if (orientationValid && poseValid)
					{
						anchor.Pose = spaceLocation.pose;
						anchor.LocateSuccess = true;
					}
				}
				else
				{
					Log.Err($"xrLocateSpace error! Result: {result}");
					anchor.LocateSuccess = false;
				}
			}
		}

		public void Shutdown()
		{

		}


		// Create Anchor
		//
		public delegate void OnCreateSuccessDel(Guid newAnchorUuid);
		public delegate void OnCreateErrorDel();

		private Dictionary<XrAsyncRequestIdFB, CreateCallback> _createCallbacks = new Dictionary<XrAsyncRequestIdFB, CreateCallback>();

		private class CreateCallback
		{
			public OnCreateSuccessDel OnSuccess;
			public OnCreateErrorDel   OnError;
		}

		public void LoadAllAnchors()
		{
			XrSpaceQueryInfoFB queryInfo = new XrSpaceQueryInfoFB();
			XrResult result = xrQuerySpacesFB(Backend.OpenXR.Session, queryInfo, out XrAsyncRequestIdFB requestId);
			if (result != XrResult.Success)
				Log.Err($"Error querying anchors! Result: {result}");
		}

		/// <summary>
		/// Create a spatial anchor at the given pose! This is an asynchronous action. You can optionally provide
		/// some callbacks for when the action completes (either success or fail). 
		/// </summary>
		/// <param name="pose">The pose where the new anchor should be created.</param>
		/// <param name="onSuccessCallback">The action to be performed when the anchor is successfully created. This is a delegate that has one Guid parameter (the new anchor id).</param>
		/// <param name="onErrorCallback">The action to perform when the create anchor fails. This is a delegate with no parameters.</param>
		/// <returns></returns>
		public bool CreateAnchor(Pose pose, OnCreateSuccessDel onSuccessCallback = null, OnCreateErrorDel onErrorCallback = null)
		{
			Log.Info("Begin CreateAnchor");

			if (!Enabled)
			{
				Log.Err("Spatial entity extension must be enabled before calling CreateAnchor!");
				return false;
			}

			XrSpatialAnchorCreateInfoFB anchorCreateInfo = new XrSpatialAnchorCreateInfoFB(Backend.OpenXR.Space, pose, Backend.OpenXR.Time);

			XrResult result = xrCreateSpatialAnchorFB(
				Backend.OpenXR.Session,
				anchorCreateInfo,
				out XrAsyncRequestIdFB requestId);

			if (result != XrResult.Success)
			{
				Log.Err($"Error requesting creation of spatial anchor: {result}");
				return false;
			}

			Log.Info($"xrCreateSpatialAnchorFB initiated. The request id is: {requestId}.");


			var createCallback = new CreateCallback
			{
				OnSuccess = onSuccessCallback,
				OnError = onErrorCallback
			};

			_createCallbacks.Add(requestId, createCallback);

			return true;
		}

		public void EraseAllAnchors()
		{
			foreach(var anchor in Anchors.Values)
			{
				// Initiate the async erase operation. Then we will remove it from the Dictionary once the event 
				// completes (in the poll handler).
				eraseSpace(anchor.XrSpace);
			}
		}

		// XR_FB_spatial_entity
		del_xrCreateSpatialAnchorFB               xrCreateSpatialAnchorFB;
		del_xrGetSpaceUuidFB                      xrGetSpaceUuidFB;
		del_xrEnumerateSpaceSupportedComponentsFB xrEnumerateSpaceSupportedComponentsFB;
		del_xrSetSpaceComponentStatusFB           xrSetSpaceComponentStatusFB;
		del_xrGetSpaceComponentStatusFB           xrGetSpaceComponentStatusFB;

		// XR_FB_spatial_entity_storage
		del_xrSaveSpaceFB  xrSaveSpaceFB;
		del_xrEraseSpaceFB xrEraseSpaceFB;

		// XR_FB_spatial_entity_query
		del_xrQuerySpacesFB xrQuerySpacesFB;
		del_xrRetrieveSpaceQueryResultsFB xrRetrieveSpaceQueryResultsFB;

		// Misc
		del_xrLocateSpace xrLocateSpace;

		bool LoadBindings()
		{
			// XR_FB_spatial_entity
			xrCreateSpatialAnchorFB = Backend.OpenXR.GetFunction<del_xrCreateSpatialAnchorFB>("xrCreateSpatialAnchorFB");
			xrGetSpaceUuidFB = Backend.OpenXR.GetFunction<del_xrGetSpaceUuidFB>("xrGetSpaceUuidFB");
			xrEnumerateSpaceSupportedComponentsFB = Backend.OpenXR.GetFunction<del_xrEnumerateSpaceSupportedComponentsFB>("xrEnumerateSpaceSupportedComponentsFB");
			xrSetSpaceComponentStatusFB = Backend.OpenXR.GetFunction<del_xrSetSpaceComponentStatusFB>("xrSetSpaceComponentStatusFB");
			xrGetSpaceComponentStatusFB = Backend.OpenXR.GetFunction<del_xrGetSpaceComponentStatusFB>("xrGetSpaceComponentStatusFB");

			// XR_FB_spatial_entity_storage
			xrSaveSpaceFB = Backend.OpenXR.GetFunction<del_xrSaveSpaceFB>("xrSaveSpaceFB");
			xrEraseSpaceFB = Backend.OpenXR.GetFunction<del_xrEraseSpaceFB>("xrEraseSpaceFB");

			// XR_FB_spatial_entity_query
			xrQuerySpacesFB = Backend.OpenXR.GetFunction<del_xrQuerySpacesFB>("xrQuerySpacesFB");
			xrRetrieveSpaceQueryResultsFB = Backend.OpenXR.GetFunction<del_xrRetrieveSpaceQueryResultsFB>("xrRetrieveSpaceQueryResultsFB");

			// Misc
			xrLocateSpace = Backend.OpenXR.GetFunction<del_xrLocateSpace>("xrLocateSpace");

			return xrCreateSpatialAnchorFB != null
				&& xrGetSpaceUuidFB != null
				&& xrEnumerateSpaceSupportedComponentsFB != null
				&& xrSetSpaceComponentStatusFB != null
				&& xrGetSpaceComponentStatusFB != null
				&& xrSaveSpaceFB != null
				&& xrEraseSpaceFB != null
				&& xrQuerySpacesFB != null
				&& xrRetrieveSpaceQueryResultsFB != null
				&& xrLocateSpace != null;
		}

		void pollEventHandler(IntPtr XrEventDataBufferData)
		{
			XrEventDataBuffer myBuffer = Marshal.PtrToStructure<XrEventDataBuffer>(XrEventDataBufferData);
			Log.Info($"xrPollEvent: received {myBuffer.type}");

			switch (myBuffer.type)
			{
				case XrStructureType.XR_TYPE_EVENT_DATA_SPATIAL_ANCHOR_CREATE_COMPLETE_FB:
					XrEventDataSpatialAnchorCreateCompleteFB spatialAnchorComplete = Marshal.PtrToStructure<XrEventDataSpatialAnchorCreateCompleteFB>(XrEventDataBufferData);

					if (!_createCallbacks.TryGetValue(spatialAnchorComplete.requestId, out CreateCallback callack))
					{
						Log.Err($"Unable to find callback for the anchor create request with Id: {spatialAnchorComplete.requestId}!");
						break;
					}

					if (spatialAnchorComplete.result != XrResult.Success)
					{
						Log.Err($"XrEventDataSpatialAnchorCreateCompleteFB error! Result: {spatialAnchorComplete.result}");
						if (callack.OnError != null)
							callack.OnError();
						break;
					}

					var newCreatedAnchor = new Anchor
					{
						XrSpace = spatialAnchorComplete.space,
					};

					Anchors.Add(spatialAnchorComplete.uuid, newCreatedAnchor);

					if (callack.OnSuccess != null)
						callack.OnSuccess(spatialAnchorComplete.uuid);

					// When anchor is first created, the component STORABLE is not yet set. So we do it here.
					if (isComponentSupported(spatialAnchorComplete.space, XrSpaceComponentTypeFB.XR_SPACE_COMPONENT_TYPE_STORABLE_FB))
					{
						XrSpaceComponentStatusSetInfoFB setComponentInfo = new XrSpaceComponentStatusSetInfoFB(XrSpaceComponentTypeFB.XR_SPACE_COMPONENT_TYPE_STORABLE_FB, true);
						XrResult setComponentResult = xrSetSpaceComponentStatusFB(spatialAnchorComplete.space, setComponentInfo, out XrAsyncRequestIdFB requestId);

						if (setComponentResult == XrResult.XR_ERROR_SPACE_COMPONENT_STATUS_ALREADY_SET_FB)
						{
							Log.Err("XR_ERROR_SPACE_COMPONENT_STATUS_ALREADY_SET_FB");

							// Save the space to storage
							saveSpace(spatialAnchorComplete.space);
						}
					}

					break;
				case XrStructureType.XR_TYPE_EVENT_DATA_SPACE_SET_STATUS_COMPLETE_FB:
					XrEventDataSpaceSetStatusCompleteFB setStatusComplete = Marshal.PtrToStructure<XrEventDataSpaceSetStatusCompleteFB>(XrEventDataBufferData);
					if (setStatusComplete.result == XrResult.Success)
					{
						if (setStatusComplete.componentType == XrSpaceComponentTypeFB.XR_SPACE_COMPONENT_TYPE_STORABLE_FB)
						{
							// Save space
							saveSpace(setStatusComplete.space);
						}
						else if (setStatusComplete.componentType == XrSpaceComponentTypeFB.XR_SPACE_COMPONENT_TYPE_LOCATABLE_FB)
						{
							// Spatial entity component was loaded from storage and component succesfully set to LOCATABLE.
							var newLoadedAnchor = new Anchor
							{
								XrSpace = setStatusComplete.space,
							};

							Anchors.Add(setStatusComplete.uuid, newLoadedAnchor);
						}
					}
					else
					{
						Log.Err("Error from XR_TYPE_EVENT_DATA_SPACE_SET_STATUS_COMPLETE_FB: " + setStatusComplete.result);
					}

					break;
				case XrStructureType.XR_TYPE_EVENT_DATA_SPACE_SAVE_COMPLETE_FB:
					XrEventDataSpaceSaveCompleteFB saveComplete = Marshal.PtrToStructure<XrEventDataSpaceSaveCompleteFB>(XrEventDataBufferData);
					if (saveComplete.result == XrResult.Success)
					{
						Log.Info("Save space sucessful!");
					}
					else
					{
						Log.Err($"Save space failed. Result: {saveComplete.result}");
						Log.Err($"XrEventDataSpaceSaveCompleteFB error! Result: {saveComplete.result}");
					}

					break;
				case XrStructureType.XR_TYPE_EVENT_DATA_SPACE_ERASE_COMPLETE_FB:
					XrEventDataSpaceEraseCompleteFB eraseComplete = Marshal.PtrToStructure<XrEventDataSpaceEraseCompleteFB>(XrEventDataBufferData);
					Anchors.Remove(eraseComplete.uuid);

					break;
				case XrStructureType.XR_TYPE_EVENT_DATA_SPACE_QUERY_RESULTS_AVAILABLE_FB:
					XrEventDataSpaceQueryResultsAvailableFB resultsAvailable = Marshal.PtrToStructure<XrEventDataSpaceQueryResultsAvailableFB>(XrEventDataBufferData);

					// Two call idiom to get memory space requirements
					XrSpaceQueryResultsFB queryResults = new XrSpaceQueryResultsFB();

					XrResult result = xrRetrieveSpaceQueryResultsFB(Backend.OpenXR.Session, resultsAvailable.requestId, out queryResults);
					if (result != XrResult.Success)
					{
						Log.Err($"xrRetrieveSpaceQueryResultsFB error! Result: {result}");
						break;
					}

					queryResults.resultCapacityInput = queryResults.resultCountOutput;

					int structByteSize = Marshal.SizeOf<XrSpaceQueryResultFB>();
					IntPtr ptr = Marshal.AllocHGlobal(structByteSize * (int)queryResults.resultCountOutput);
					queryResults.results = ptr;
					result = xrRetrieveSpaceQueryResultsFB(Backend.OpenXR.Session, resultsAvailable.requestId, out queryResults);

					if (result != XrResult.Success)
					{
						Log.Err($"xrRetrieveSpaceQueryResultsFB error! Result: {result}");
						break;
					}

					Log.Info($"Spatial entity loaded from storage! Count: {queryResults.resultCountOutput}");

					// Copy the results into managed memory (since it's easier to work with)
					List<XrSpaceQueryResultFB> resultsList = new List<XrSpaceQueryResultFB>();
					for (int i = 0; i < queryResults.resultCountOutput; i++)
					{
						XrSpaceQueryResultFB res = Marshal.PtrToStructure<XrSpaceQueryResultFB>(queryResults.results + (i * structByteSize));
						resultsList.Add(res);
					}	

					Marshal.FreeHGlobal(ptr);

					resultsList.ForEach(res =>
					{
						// Set component LOCATABLE
						if (isComponentSupported(res.space, XrSpaceComponentTypeFB.XR_SPACE_COMPONENT_TYPE_LOCATABLE_FB))
						{
							XrSpaceComponentStatusSetInfoFB setComponentInfo = new XrSpaceComponentStatusSetInfoFB(XrSpaceComponentTypeFB.XR_SPACE_COMPONENT_TYPE_LOCATABLE_FB, true);
							XrResult setComponentResult = xrSetSpaceComponentStatusFB(res.space, setComponentInfo, out XrAsyncRequestIdFB requestId);

							if (setComponentResult != XrResult.Success)
								Log.Err($"xrSetSpaceComponentStatusFB error! Result: {setComponentResult}");

							// Once this async event XR_TYPE_EVENT_DATA_SPACE_SET_STATUS_COMPLETE_FB is posted (above), this spatial entity will
							// be added to the application's Anchor list!
						}

						// Set component SORTABLE
						if (isComponentSupported(res.space, XrSpaceComponentTypeFB.XR_SPACE_COMPONENT_TYPE_STORABLE_FB))
						{
							XrSpaceComponentStatusSetInfoFB setComponentInfo = new XrSpaceComponentStatusSetInfoFB(XrSpaceComponentTypeFB.XR_SPACE_COMPONENT_TYPE_STORABLE_FB, true);
							XrResult setComponentResult = xrSetSpaceComponentStatusFB(res.space, setComponentInfo, out XrAsyncRequestIdFB requestId);

							if (setComponentResult != XrResult.Success)
								Log.Err($"xrSetSpaceComponentStatusFB error! Result: {setComponentResult}");
						}
					});

					break;
				case XrStructureType.XR_TYPE_EVENT_DATA_SPACE_QUERY_COMPLETE_FB:
					break;
				default:
					break;
			}
		}

		bool isComponentSupported(XrSpace space, XrSpaceComponentTypeFB type)
		{
			// Two call idiom to get memory space requirements
			uint numComponents = 0;
			XrResult result = xrEnumerateSpaceSupportedComponentsFB(space, 0, out numComponents, null);

			XrSpaceComponentTypeFB[] componentTypes = new XrSpaceComponentTypeFB[numComponents];
			result = xrEnumerateSpaceSupportedComponentsFB(space, numComponents, out numComponents, componentTypes);

			if (result != XrResult.Success)
			{
				Log.Err($"xrEnumerateSpaceSupportedComponentsFB Error! Result: {result}");
			}

			for (int i = 0; i < numComponents; i++)
			{
				if (componentTypes[i] == type)
					return true;
			}

			return false;
		}

		void saveSpace(XrSpace space)
		{
			XrSpaceSaveInfoFB saveInfo = new XrSpaceSaveInfoFB(space, XrSpaceStorageLocationFB.XR_SPACE_STORAGE_LOCATION_LOCAL_FB, XrSpacePersistenceModeFB.XR_SPACE_PERSISTENCE_MODE_INDEFINITE_FB);
			XrResult result = xrSaveSpaceFB(Backend.OpenXR.Session, saveInfo, out XrAsyncRequestIdFB requestId);
			if (result != XrResult.Success)
				Log.Err($"xrSaveSpaceFB! Result: {result}");
		}

		void eraseSpace(XrSpace space)
		{
			XrSpaceEraseInfoFB eraseInfo = new XrSpaceEraseInfoFB(space, XrSpaceStorageLocationFB.XR_SPACE_STORAGE_LOCATION_LOCAL_FB);
			XrResult result = xrEraseSpaceFB(Backend.OpenXR.Session, eraseInfo, out XrAsyncRequestIdFB requestId);
			if (result != XrResult.Success)
				Log.Err($"xrEraseSpaceFB! Result: {result}");
		}
	}
}