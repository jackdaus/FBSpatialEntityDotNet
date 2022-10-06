using System;
using System.Runtime.InteropServices;
using XrAsyncRequestIdFB = System.UInt64;

namespace StereoKit.Framework
{
	class PassthroughFBExt2 : IStepper
	{
		bool extAvailable;
		bool enabled;
		bool enabledPassthrough;
		bool enableOnInitialize;
		bool passthroughRunning;
		XrPassthroughFB activePassthrough = new XrPassthroughFB();
		XrPassthroughLayerFB activeLayer = new XrPassthroughLayerFB();

		Color oldColor;
		bool oldSky;

		// SPATIAL_ENTITY
		bool spatialExtAvailable;


		public bool Available => extAvailable;
		public bool SpatialAvailable => spatialExtAvailable;
		public bool Enabled { get => extAvailable && enabled; set => enabled = value; }
		public bool EnabledPassthrough
		{
			get => enabledPassthrough; set
			{
				if (Available && enabledPassthrough != value)
				{
					enabledPassthrough = value;
					if (enabledPassthrough) StartPassthrough();
					if (!enabledPassthrough) EndPassthrough();
				}
			}
		}

		public PassthroughFBExt2() : this(true) { }
		public PassthroughFBExt2(bool enabled = true)
		{
			if (SK.IsInitialized)
				Log.Err("PassthroughFBExt must be constructed before StereoKit is initialized!");
			Backend.OpenXR.RequestExt("XR_EXT_uuid");
			Backend.OpenXR.RequestExt("XR_FB_spatial_entity");
			Backend.OpenXR.RequestExt("XR_FB_passthrough");
			enableOnInitialize = enabled;
		}

		public bool Initialize()
		{
			//extAvailable =
			//	Backend.XRType == BackendXRType.OpenXR &&
			//	Backend.OpenXR.ExtEnabled("XR_FB_passthrough") &&firefox
			//	LoadBindings();

			bool isOpenXR = Backend.XRType == BackendXRType.OpenXR;
			bool isExtEnabled = Backend.OpenXR.ExtEnabled("XR_FB_passthrough");
			bool isLoadBindingsSuccessful = LoadBindings();


			bool isSpatialExtEnabled = Backend.OpenXR.ExtEnabled("XR_FB_spatial_entity");
			bool isLoadSpatialBindingsSuccessful = LoadSpatialBindings();
			Log.Info($"LoadSpatialBindings: {(isLoadSpatialBindingsSuccessful ? "SUCCESS" : "FAIL")}");

			extAvailable = isOpenXR && isExtEnabled && isLoadBindingsSuccessful;
			spatialExtAvailable = isOpenXR && isSpatialExtEnabled && isLoadSpatialBindingsSuccessful;

			if (enableOnInitialize)
				EnabledPassthrough = true;
			return true;
		}

		public void Step()
		{
			if (!EnabledPassthrough) return;

			XrCompositionLayerPassthroughFB layer = new XrCompositionLayerPassthroughFB(
				XrCompositionLayerFlags.BLEND_TEXTURE_SOURCE_ALPHA_BIT, activeLayer);
			Backend.OpenXR.AddCompositionLayer(layer, -1);
		}

		public void Shutdown()
		{
			EnabledPassthrough = false;
		}

		void StartPassthrough()
		{
			if (!extAvailable) return;
			if (passthroughRunning) return;
			passthroughRunning = true;

			oldColor = Renderer.ClearColor;
			oldSky = Renderer.EnableSky;

			XrResult result = xrCreatePassthroughFB(
				Backend.OpenXR.Session,
				new XrPassthroughCreateInfoFB(XrPassthroughFlagsFB.IS_RUNNING_AT_CREATION_BIT_FB),
				out activePassthrough);

			result = xrCreatePassthroughLayerFB(
				Backend.OpenXR.Session,
				new XrPassthroughLayerCreateInfoFB(activePassthrough, XrPassthroughFlagsFB.IS_RUNNING_AT_CREATION_BIT_FB, XrPassthroughLayerPurposeFB.RECONSTRUCTION_FB),
				out activeLayer);

			Renderer.ClearColor = Color.BlackTransparent;
			Renderer.EnableSky = false;
		}

		void EndPassthrough()
		{
			if (!passthroughRunning) return;
			passthroughRunning = false;

			xrPassthroughPauseFB(activePassthrough);
			xrDestroyPassthroughLayerFB(activeLayer);
			xrDestroyPassthroughFB(activePassthrough);

			Renderer.ClearColor = oldColor;
			Renderer.EnableSky = oldSky;
		}

		// SPATIAL_EXT
		public bool CreateAnchor()
		{
			Log.Info("Begin CreateAnchor");

			var anchorCreateInfo = new XrSpatialAnchorCreateInfoFB();
			anchorCreateInfo.next = IntPtr.Zero;
			anchorCreateInfo.space = Backend.OpenXR.Space;
			//anchorCreateInfo.poseInSpace = Pose.Identity; // TODO replace with hand location
			anchorCreateInfo.poseInSpace.orientation.w = 1;
            anchorCreateInfo.time = Backend.OpenXR.Time;




            XrResult result = xrCreateSpatialAnchorFB(
				Backend.OpenXR.Session,
				anchorCreateInfo,
				out XrAsyncRequestIdFB requestId);

            Log.Info($"xrCreateSpatialAnchorFB initiated. The request id is: {requestId}. Result: {result.ToString()}");

			return result == XrResult.Success;
		}


		#region OpenXR native bindings and types
		enum XrStructureType : UInt64
		{
			XR_TYPE_PASSTHROUGH_CREATE_INFO_FB = 1000118001,
			XR_TYPE_PASSTHROUGH_LAYER_CREATE_INFO_FB = 1000118002,
			XR_TYPE_PASSTHROUGH_STYLE_FB = 1000118020,
			XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_FB = 1000118003,

			// SPATIAL_EXT New Enum Constants
			XR_TYPE_SYSTEM_SPATIAL_ENTITY_PROPERTIES_FB = 1000113004,
			XR_TYPE_SPATIAL_ANCHOR_CREATE_INFO_FB = 1000011303,
		}
		enum XrPassthroughFlagsFB : UInt64
		{
			None = 0,
			IS_RUNNING_AT_CREATION_BIT_FB = 0x00000001
		}
		enum XrCompositionLayerFlags : UInt64
		{
			None = 0,
			CORRECT_CHROMATIC_ABERRATION_BIT = 0x00000001,
			BLEND_TEXTURE_SOURCE_ALPHA_BIT = 0x00000002,
			UNPREMULTIPLIED_ALPHA_BIT = 0x00000004,
		}
		enum XrPassthroughLayerPurposeFB : UInt32
		{
			RECONSTRUCTION_FB = 0,
			PROJECTED_FB = 1,
			TRACKED_KEYBOARD_HANDS_FB = 1000203001,
			MAX_ENUM_FB = 0x7FFFFFFF,
		}
		enum XrResult : Int32
		{
			Success = 0,
			XR_ERROR_VALIDATION_FAILURE = -1,
			// Provided by XR_FB_spatial_entity
			XR_ERROR_SPACE_COMPONENT_NOT_SUPPORTED_FB = -1000113000,
			// Provided by XR_FB_spatial_entity
			XR_ERROR_SPACE_COMPONENT_NOT_ENABLED_FB = -1000113001,
			// Provided by XR_FB_spatial_entity
			XR_ERROR_SPACE_COMPONENT_STATUS_PENDING_FB = -1000113002,
			// Provided by XR_FB_spatial_entity
			XR_ERROR_SPACE_COMPONENT_STATUS_ALREADY_SET_FB = -1000113003,
		}

		// SPATIAL_EXT New Enums
		enum XrSpaceComponentTypeFB : UInt32
		{
			XR_SPACE_COMPONENT_TYPE_LOCATABLE_FB = 0,
			XR_SPACE_COMPONENT_TYPE_STORABLE_FB = 1,
			XR_SPACE_COMPONENT_TYPE_BOUNDED_2D_FB = 3,
			XR_SPACE_COMPONENT_TYPE_BOUNDED_3D_FB = 4,
			XR_SPACE_COMPONENT_TYPE_SEMANTIC_LABELS_FB = 5,
			XR_SPACE_COMPONENT_TYPE_ROOM_LAYOUT_FB = 6,
			XR_SPACE_COMPONENT_TYPE_SPACE_CONTAINER_FB = 7,
			XR_SPACE_COMPONENT_TYPE_MAX_ENUM_FB = 0x7FFFFFFF
		}


#pragma warning disable 0169 // handle is not "used", but required for interop
		struct XrPassthroughFB { ulong handle; }
		struct XrPassthroughLayerFB { ulong handle; }

		// SPATIAL_EXT New Base Types
		/// <summary>
		/// Represents a request to the spatial entity system. Several functions in this and other 
		/// extensions will populate an output variable of this type so that an application can 
		/// use it when referring to a specific request.
		/// </summary>
		//struct XrAsyncRequestIdFB { public ulong handle; }
#pragma warning restore 0169

		[StructLayout(LayoutKind.Sequential)]
		struct XrPassthroughCreateInfoFB
		{
			private XrStructureType type;
			public IntPtr next;
			public XrPassthroughFlagsFB flags;

			public XrPassthroughCreateInfoFB(XrPassthroughFlagsFB passthroughFlags)
			{
				type = XrStructureType.XR_TYPE_PASSTHROUGH_CREATE_INFO_FB;
				next = IntPtr.Zero;
				flags = passthroughFlags;
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		struct XrPassthroughLayerCreateInfoFB
		{
			private XrStructureType type;
			public IntPtr next;
			public XrPassthroughFB passthrough;
			public XrPassthroughFlagsFB flags;
			public XrPassthroughLayerPurposeFB purpose;

			public XrPassthroughLayerCreateInfoFB(XrPassthroughFB passthrough, XrPassthroughFlagsFB flags, XrPassthroughLayerPurposeFB purpose)
			{
				type = XrStructureType.XR_TYPE_PASSTHROUGH_LAYER_CREATE_INFO_FB;
				next = IntPtr.Zero;
				this.passthrough = passthrough;
				this.flags = flags;
				this.purpose = purpose;
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		struct XrPassthroughStyleFB
		{
			public XrStructureType type;
			public IntPtr next;
			public float textureOpacityFactor;
			public Color edgeColor;
			public XrPassthroughStyleFB(float textureOpacityFactor, Color edgeColor)
			{
				type = XrStructureType.XR_TYPE_PASSTHROUGH_STYLE_FB;
				next = IntPtr.Zero;
				this.textureOpacityFactor = textureOpacityFactor;
				this.edgeColor = edgeColor;
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		struct XrCompositionLayerPassthroughFB
		{
			public XrStructureType type;
			public IntPtr next;
			public XrCompositionLayerFlags flags;
			public ulong space;
			public XrPassthroughLayerFB layerHandle;
			public XrCompositionLayerPassthroughFB(XrCompositionLayerFlags flags, XrPassthroughLayerFB layerHandle)
			{
				type = XrStructureType.XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_FB;
				next = IntPtr.Zero;
				space = 0;
				this.flags = flags;
				this.layerHandle = layerHandle;
			}
		}

		// SPATIAL_EXT New Structures
		[StructLayout(LayoutKind.Sequential)]
		struct XrSystemSpatialEntityPropertiesFB
		{
			private XrStructureType type;           // TODO should this be private or public?
			public IntPtr next;
			public Boolean supportsSpatialEntity;   // a boolean value that determines if spatial entities are supported by the system.

			public XrSystemSpatialEntityPropertiesFB(Boolean supportsSpatialEntity) // TODO not sure if we need supportsSpatialEntity in the constructor...
			{
				type = XrStructureType.XR_TYPE_SYSTEM_SPATIAL_ENTITY_PROPERTIES_FB;
				next = IntPtr.Zero;
				this.supportsSpatialEntity = supportsSpatialEntity;
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		struct XrSpatialAnchorCreateInfoFB
		{
			private XrStructureType type;           // TODO should this be private or public?
			public IntPtr next;
			public UInt64 space;        // not sure if this is the correct type...
			//public Pose poseInSpace;    // use Pose?
			public XrPosef poseInSpace;    // use Pose?
            public Int64 time;          // typedef int64_t XrTime;

			public XrSpatialAnchorCreateInfoFB(UInt64 space, XrPosef poseInSpace, Int64 time)
			{
				type = XrStructureType.XR_TYPE_SPATIAL_ANCHOR_CREATE_INFO_FB;
				next = IntPtr.Zero;

				this.space = space;
				this.poseInSpace = poseInSpace;
				this.time = time;
			}
		}


		delegate XrResult del_xrCreatePassthroughFB(ulong session, [In] XrPassthroughCreateInfoFB createInfo, out XrPassthroughFB outPassthrough);
		delegate XrResult del_xrDestroyPassthroughFB(XrPassthroughFB passthrough);
		delegate XrResult del_xrPassthroughStartFB(XrPassthroughFB passthrough);
		delegate XrResult del_xrPassthroughPauseFB(XrPassthroughFB passthrough);
		delegate XrResult del_xrCreatePassthroughLayerFB(ulong session, [In] XrPassthroughLayerCreateInfoFB createInfo, out XrPassthroughLayerFB outLayer);
		delegate XrResult del_xrDestroyPassthroughLayerFB(XrPassthroughLayerFB layer);
		delegate XrResult del_xrPassthroughLayerPauseFB(XrPassthroughLayerFB layer);
		delegate XrResult del_xrPassthroughLayerResumeFB(XrPassthroughLayerFB layer);
		delegate XrResult del_xrPassthroughLayerSetStyleFB(XrPassthroughLayerFB layer, [In] XrPassthroughStyleFB style);

		// SPATIAL_EXT New Functions
		//delegate XrResult del_xrCreateSpatialAnchorFB(ulong session, [In] XrSpatialAnchorCreateInfoFB info, out XrAsyncRequestIdFB requestId);
		delegate XrResult del_xrCreateSpatialAnchorFB(ulong session, [In] XrSpatialAnchorCreateInfoFB info, out XrAsyncRequestIdFB requestId);


        del_xrCreatePassthroughFB xrCreatePassthroughFB;
		del_xrDestroyPassthroughFB xrDestroyPassthroughFB;
		del_xrPassthroughStartFB xrPassthroughStartFB;
		del_xrPassthroughPauseFB xrPassthroughPauseFB;
		del_xrCreatePassthroughLayerFB xrCreatePassthroughLayerFB;
		del_xrDestroyPassthroughLayerFB xrDestroyPassthroughLayerFB;
		// I don't think these three below are used in this example
		del_xrPassthroughLayerPauseFB xrPassthroughLayerPauseFB;
		del_xrPassthroughLayerResumeFB xrPassthroughLayerResumeFB;
		del_xrPassthroughLayerSetStyleFB xrPassthroughLayerSetStyleFB;

		// SPATIAL_EXT New Functions
		del_xrCreateSpatialAnchorFB xrCreateSpatialAnchorFB;


		bool LoadBindings()
		{
			xrCreatePassthroughFB = Backend.OpenXR.GetFunction<del_xrCreatePassthroughFB>("xrCreatePassthroughFB");
			xrDestroyPassthroughFB = Backend.OpenXR.GetFunction<del_xrDestroyPassthroughFB>("xrDestroyPassthroughFB");
			xrPassthroughStartFB = Backend.OpenXR.GetFunction<del_xrPassthroughStartFB>("xrPassthroughStartFB");
			xrPassthroughPauseFB = Backend.OpenXR.GetFunction<del_xrPassthroughPauseFB>("xrPassthroughPauseFB");
			xrCreatePassthroughLayerFB = Backend.OpenXR.GetFunction<del_xrCreatePassthroughLayerFB>("xrCreatePassthroughLayerFB");
			xrDestroyPassthroughLayerFB = Backend.OpenXR.GetFunction<del_xrDestroyPassthroughLayerFB>("xrDestroyPassthroughLayerFB");
			xrPassthroughLayerPauseFB = Backend.OpenXR.GetFunction<del_xrPassthroughLayerPauseFB>("xrPassthroughLayerPauseFB");
			xrPassthroughLayerResumeFB = Backend.OpenXR.GetFunction<del_xrPassthroughLayerResumeFB>("xrPassthroughLayerResumeFB");
			xrPassthroughLayerSetStyleFB = Backend.OpenXR.GetFunction<del_xrPassthroughLayerSetStyleFB>("xrPassthroughLayerSetStyleFB");

			return
				xrCreatePassthroughFB != null &&
				xrDestroyPassthroughFB != null &&
				xrPassthroughStartFB != null &&
				xrPassthroughPauseFB != null &&
				xrCreatePassthroughLayerFB != null &&
				xrDestroyPassthroughLayerFB != null &&
				xrPassthroughLayerPauseFB != null &&
				xrPassthroughLayerResumeFB != null &&
				xrPassthroughLayerSetStyleFB != null;
		}

		// SPATIAL_ENTITY
		bool LoadSpatialBindings()
		{
			//XR_FB_spatial_entity
			xrCreateSpatialAnchorFB = Backend.OpenXR.GetFunction<del_xrCreateSpatialAnchorFB>("xrCreateSpatialAnchorFB");

			return xrCreateSpatialAnchorFB != null;
		}
		#endregion

		[StructLayout(LayoutKind.Sequential)]
		struct XrPosef
		{
            public XrQuaternionf orientation;
            public XrVector3f position;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct XrQuaternionf
		{
            public float x;
            public float y;
            public float z;
            public float w;
		}

        [StructLayout(LayoutKind.Sequential)]
        struct XrVector3f
        {
            public float x;
            public float y;
            public float z;
        }
    }
}