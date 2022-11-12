using StereoKit;
using System;
using System.Runtime.InteropServices;

using XrSpace = System.UInt64;
using XrTime = System.Int64;
using XrDuration = System.Int64;
using XrAsyncRequestIdFB = System.UInt64;

namespace StereoKit.Framework
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

	// XR_EXT_uuid
	[StructLayout(LayoutKind.Sequential)]
	struct XrUuidEXT
	{
		// TODO not sure if this is correct
		public byte[] data;

		public XrUuidEXT()
		{
			data = new byte[16];
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

		/// <summary>
		/// the component whose status is to be set
		/// </summary>
		public XrSpaceComponentTypeFB componentType;

		public bool enabled;

		/// <summary>
		/// the number of nanoseconds before the operation should be cancelled. A value of XR_INFINITE_DURATION 
		/// indicates to never time out.
		/// </summary>
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

		public XrEventDataSpatialAnchorCreateCompleteFB()
		{
			type = XrStructureType.XR_TYPE_EVENT_DATA_SPATIAL_ANCHOR_CREATE_COMPLETE_FB;
			next = IntPtr.Zero;
			requestId = 0;
			result = 0;
			space = 0;
			uuid = Guid.Empty;
			//uuid = new XrUuidEXT();
		}
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
		public XrUuidEXT uuid;
		public XrSpaceComponentTypeFB componentType;

		/// <summary>
		/// a boolean value indicating whether the component is now enabled or disabled.
		/// </summary>
		public bool enabled;

		public XrEventDataSpaceSetStatusCompleteFB()
		{
			type = XrStructureType.XR_TYPE_EVENT_DATA_SPACE_SET_STATUS_COMPLETE_FB;
			next = IntPtr.Zero;
			requestId = 0;
			result = 0;
			space = 0;
			uuid = new XrUuidEXT();
			componentType = 0;
			enabled = false;
		}
	}

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
