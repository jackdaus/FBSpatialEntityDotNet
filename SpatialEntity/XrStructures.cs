using StereoKit;
using System;
using System.Runtime.InteropServices;

using XrTime             = System.Int64;
using XrDuration         = System.Int64;
using XrSpace            = System.UInt64;
using XrAsyncRequestIdFB = System.UInt64;
 
namespace SpatialEntity
{
	[StructLayout(LayoutKind.Sequential)]
	struct XrEventDataBuffer
	{
		public XrStructureType type;
		public IntPtr next;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4000)]
		public byte[] flags;

		public XrEventDataBuffer()
		{
			type = XrStructureType.XR_TYPE_EVENT_DATA_BUFFER;
			next = IntPtr.Zero;
			flags = new byte[4000];
		}
	}

	#region XR_FB_spatial_entity

	[StructLayout(LayoutKind.Sequential)]
	struct XrSystemSpatialEntityPropertiesFB
	{
		private XrStructureType type;
		public IntPtr next;

		/// <summary>
		/// a boolean value that determines if spatial entities are supported by the system.
		/// </summary>
		public bool supportsSpatialEntity;

		public XrSystemSpatialEntityPropertiesFB(bool supportsSpatialEntity)
		{
			type = XrStructureType.XR_TYPE_SYSTEM_SPATIAL_ENTITY_PROPERTIES_FB;
			next = IntPtr.Zero;
			this.supportsSpatialEntity = supportsSpatialEntity;
		}
	}

	/// <summary>
	/// Parameters to create a new spatial anchor
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpatialAnchorCreateInfoFB
	{
		private XrStructureType type;
		public IntPtr next;
		public XrSpace space;
		public XrPosef poseInSpace;
		public XrTime time;

		public XrSpatialAnchorCreateInfoFB(XrSpace space, XrPosef poseInSpace, XrTime time)
		{
			type = XrStructureType.XR_TYPE_SPATIAL_ANCHOR_CREATE_INFO_FB;
			next = IntPtr.Zero;
			this.space = space;
			this.poseInSpace = poseInSpace;
			this.time = time;
		}
	}

	/// <summary>
	/// Enables or disables the specified component for the specified spatial entity
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceComponentStatusSetInfoFB
	{
		private XrStructureType type;
		public IntPtr next;
		public XrSpaceComponentTypeFB componentType;
		public bool enabled;
		public XrDuration timeout;

		public XrSpaceComponentStatusSetInfoFB(XrSpaceComponentTypeFB componentType, bool enabled)
		{
			type = XrStructureType.XR_TYPE_SPACE_COMPONENT_STATUS_SET_INFO_FB;
			next = IntPtr.Zero;
			this.componentType = componentType;
			this.enabled = enabled;
			timeout = XrConstants.XR_INFINITE_DURATION;
		}
	}

	/// <summary>
	/// Holds information on the current state of a component.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceComponentStatusFB
	{
		XrStructureType type;
		public IntPtr next;

		/// <summary>
		/// a boolean value that determines if a component is currently enabled or disabled.
		/// </summary>
		public bool enabled;

		/// <summary>
		/// a boolean value that determines if the component’s enabled state is about to change.
		/// </summary>
		public bool changePending;

		public XrSpaceComponentStatusFB()
		{
			type = XrStructureType.XR_TYPE_SPACE_COMPONENT_STATUS_FB;
			next = IntPtr.Zero;
			enabled = false;
			changePending = false;
		}
	}

	/// <summary>
	/// It describes the result of a request to create a new spatial anchor. Once this event is posted,
	/// it is the application's responsibility to take ownership of the XrSpace. The XrSession passed 
	/// into xrCreateSpatialAnchorFB is the parent handle of the newly created XrSpace.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrEventDataSpatialAnchorCreateCompleteFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrAsyncRequestIdFB requestId;
		public XrResult result;
		public XrSpace space;
		public Guid uuid; // NOTE: not sure if this data is  Marshalled correctly... need to test
		//public XrUuidEXT uuid;
	}

	/// <summary>
	/// Describes the result of a request to enable or disable a component of a spatial entity.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrEventDataSpaceSetStatusCompleteFB
	{
		private XrStructureType type;
		public IntPtr next;
		public XrAsyncRequestIdFB requestId;
		public XrResult result;
		public XrSpace space;
		public Guid uuid;
		public XrSpaceComponentTypeFB componentType;
		public bool enabled;
	}

	#endregion


	#region XR_FB_spatial_entity_storage

	/// <summary>
	/// Contains information used to save the spatial entity.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceSaveInfoFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrSpace space;
		public XrSpaceStorageLocationFB location;
		public XrSpacePersistenceModeFB persistenceMode;

		public XrSpaceSaveInfoFB(XrSpace space, XrSpaceStorageLocationFB location, XrSpacePersistenceModeFB persistenceMode)
		{
			type = XrStructureType.XR_TYPE_SPACE_SAVE_INFO_FB;
			next = IntPtr.Zero;
			this.space = space;
			this.location = location;
			this.persistenceMode = persistenceMode;
		}
	}

	/// <summary>
	/// Contains information used to erase the spatial entity.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceEraseInfoFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrSpace space;
		public XrSpaceStorageLocationFB location;

		public XrSpaceEraseInfoFB(XrSpace space, XrSpaceStorageLocationFB location)
		{
			type = XrStructureType.XR_TYPE_SPACE_ERASE_INFO_FB;
			next = IntPtr.Zero;
			this.space = space;
			this.location = location;
		}
	}

	/// <summary>
	/// The save result event contains the success of the save/write operation to the specified location, 
	/// as well as the XrSpace handle on which the save operation was attempted on, the unique UUID, and 
	/// the triggered async request ID from the initial calling function.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrEventDataSpaceSaveCompleteFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrAsyncRequestIdFB requestId;
		public XrResult result;
		public XrSpace space;
		public Guid uuid;
		public XrSpaceStorageLocationFB location;
	}

	/// <summary>
	/// The erase result event contains the success of the erase operation from the specified storage 
	/// location. It also provides the UUID of the entity and the async request ID from the initial 
	/// calling function.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrEventDataSpaceEraseCompleteFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrAsyncRequestIdFB requestId;
		public XrResult result;
		public XrSpace space;
		public Guid uuid;
		public XrSpaceStorageLocationFB location;
	}

	#endregion


	#region XR_FB_spatial_entity_query

	/// <summary>
	/// A base structure that is not intended to be directly used, but forms a basis for specific filter 
	/// info types.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceFilterInfoBaseHeaderFB
	{
		public XrStructureType type;
		public IntPtr next;
	}

	/// <summary>
	/// Used to query for spaces and perform a specific action on the spaces returned.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceQueryInfoFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrSpaceQueryActionFB queryAction;
		public UInt32 maxResultCount;
		public XrDuration timeout;
		public IntPtr filter;        // Pointer of type XrSpaceFilterInfoBaseHeaderFB
		public IntPtr excludeFilter; // Pointer of type XrSpaceFilterInfoBaseHeaderFB

		public XrSpaceQueryInfoFB()
		{
			type = XrStructureType.XR_TYPE_SPACE_QUERY_INFO_FB;
			next = IntPtr.Zero;
			queryAction = XrSpaceQueryActionFB.XR_SPACE_QUERY_ACTION_LOAD_FB;
			maxResultCount = 20;
			//timeout = XrConstants.XR_INFINITE_DURATION; // leads to validation error...?!
			timeout = 0;
			filter = IntPtr.Zero;
			excludeFilter = IntPtr.Zero;
		}
	}

	/// <summary>
	/// Extends a query filter to limit a query to a specific storage location.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceStorageLocationFilterInfoFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrSpaceStorageLocationFB location;

		public XrSpaceStorageLocationFilterInfoFB()
		{
			type = XrStructureType.XR_TYPE_SPACE_STORAGE_LOCATION_FILTER_INFO_FB;
			next = IntPtr.Zero;
			location = XrSpaceStorageLocationFB.XR_SPACE_STORAGE_LOCATION_LOCAL_FB;
		}
	}

	/// <summary>
	/// A query result returned in the results output parameter of the xrRetrieveSpaceQueryResultsFB function.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceQueryResultFB
	{
		public XrSpace space;
		public Guid uuid;
	}

	/// <summary>
	/// Used by the xrRetrieveSpaceQueryResultsFB function to retrieve query results.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceQueryResultsFB
	{
		public XrStructureType type;
		public IntPtr next;
		public UInt32 resultCapacityInput;
		public UInt32 resultCountOutput;
		public IntPtr results; // Pointer to XrSpaceQueryResultFB[]

		public XrSpaceQueryResultsFB()
		{
			type = XrStructureType.XR_TYPE_SPACE_QUERY_RESULTS_FB;
			next = IntPtr.Zero;
			resultCapacityInput = 0;
			resultCountOutput = 0;
			results = IntPtr.Zero;
		}
	}

	/// <summary>
	/// It indicates a query request has produced some number of results. If a query yields 
	/// results this event MUST be delivered before the XrEventDataSpaceQueryCompleteFB event 
	/// is delivered. Call xrQuerySpacesFB to retrieve those results.
	/// </summary>
	struct XrEventDataSpaceQueryResultsAvailableFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrAsyncRequestIdFB requestId;
	}

	/// <summary>
	/// It indicates a query request has completed and specifies the request result. This event 
	/// must be delivered when a query has completed, regardless of the number of results found. 
	/// If any results have been found, then this event must be delivered after any 
	/// XrEventDataSpaceQueryResultsAvailableFB events have been delivered.
	/// </summary>
	struct XrEventDataSpaceQueryCompleteFB
	{
		public XrStructureType type;
		public IntPtr next;
		public XrAsyncRequestIdFB requestId;
		public XrResult result;
	}

	// Not Implemented:
	//
	// - struct XrSpaceUuidFilterInfoFB 
	// - struct XrSpaceComponentFilterInfoFB 

	#endregion


	#region Other structs

	[StructLayout(LayoutKind.Sequential)]
	struct XrSpaceLocation
	{
		public XrStructureType type;
		public IntPtr next;
		public XrSpaceLocationFlags locationFlags;
		public XrPosef pose;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct XrPosef
	{
		public XrQuaternionf orientation;
		public XrVector3f position;

		public static implicit operator Pose(XrPosef p) => new Pose
		{
			position = new Vec3(p.position.x, p.position.y, p.position.z),
			orientation = new Quat(p.orientation.x, p.orientation.y, p.orientation.z, p.orientation.w)
		};

		public static implicit operator XrPosef(Pose p) => new XrPosef
		{
			orientation = new XrQuaternionf(p.orientation.x, p.orientation.y, p.orientation.z, p.orientation.w),
			position = new XrVector3f(p.position.x, p.position.y, p.position.z),
		};
	}

	[StructLayout(LayoutKind.Sequential)]
	struct XrQuaternionf
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public XrQuaternionf()
		{
			x = 0; y = 0; z = 0; w = 0;
		}

		public XrQuaternionf(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct XrVector3f
	{
		public float x;
		public float y;
		public float z;

		public XrVector3f()
		{
			x = 0; y = 0; z = 0;
		}

		public XrVector3f(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	#endregion
}
