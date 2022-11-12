using System.Runtime.InteropServices;

using XrSpace = System.UInt64;
using XrTime = System.Int64;
using XrAsyncRequestIdFB = System.UInt64;

namespace StereoKit.Framework
{
	#region XR_FB_spatial_entity

	/// <summary>
	/// Creates a Spatial Anchor using the specified tracking origin and pose relative to the specified tracking origin.
	/// </summary>
	/// <param name="session"></param>
	/// <param name="info"></param>
	/// <param name="requestId"></param>
	/// <returns></returns>
	delegate XrResult del_xrCreateSpatialAnchorFB(
		ulong session,
		[In] XrSpatialAnchorCreateInfoFB info,
		out XrAsyncRequestIdFB requestId);

	/// <summary>
	/// Gets the UUID for a spatial entity.
	/// </summary>
	/// <param name="space">The XrSpace handle of a spatial entity.</param>
	/// <param name="uuid">An output parameter pointing to the entity’s UUID.</param>
	/// <returns></returns>
	delegate XrResult del_xrGetSpaceUuidFB(
		XrSpace space,
		out XrUuidEXT uuid);

	/// <summary>
	/// Lists any component types that an entity supports.
	/// </summary>
	/// <param name="space">the XrSpace handle to the spatial entity.</param>
	/// <param name="componentTypeCapacityInput">the capacity of the componentTypes array, or 0 to indicate a request to retrieve the required capacity.</param>
	/// <param name="componentTypeCountOutput">a pointer to the count of componentTypes written, or a pointer to the required capacity in the case that componentTypeCapacityInput is insufficient.</param>
	/// <param name="componentTypes">a pointer to an array of XrSpaceComponentTypeFB values, but can be NULL if componentTypeCapacityInput is 0.</param>
	/// <returns></returns>
	delegate XrResult del_xrEnumerateSpaceSupportedComponentsFB(
		XrSpace space,
		uint componentTypeCapacityInput,
		out uint componentTypeCountOutput,
		XrSpaceComponentTypeFB[] componentTypes);           // TODO not sure if this is correct...


	/// <summary>
	/// Enables or disables the specified component for the specified entity.
	/// </summary>
	/// <param name="space">the XrSpace handle to the spatial entity.</param>
	/// <param name="info">a pointer to an XrSpaceComponentStatusSetInfoFB structure containing information about the component to be enabled or disabled.</param>
	/// <param name="requestId">the output parameter that points to the ID of this asynchronous request.</param>
	/// <returns></returns>
	delegate XrResult del_xrSetSpaceComponentStatusFB(
		XrSpace space,
		[In] XrSpaceComponentStatusSetInfoFB info,
		out XrAsyncRequestIdFB requestId);

	/// <summary>
	/// Gets the current status of the specified component for the specified entity.
	/// </summary>
	/// <param name="space">the XrSpace handle of a spatial entity.</param>
	/// <param name="componentType">the component type to query.</param>
	/// <param name="status">an output parameter pointing to the structure containing the status of the component that was queried.</param>
	/// <returns></returns>
	delegate XrResult del_xrGetSpaceComponentStatusFB(
		XrSpace space,
		XrSpaceComponentTypeFB componentType,
		out XrSpaceComponentStatusFB status);

	#endregion


	#region Other XrFunctions
	
	delegate XrResult del_xrLocateSpace(
		XrSpace space,
		XrSpace baseSpace,
		XrTime time,
		out XrSpaceLocation location);

	#endregion
}
