using ARInventory;
using StereoKit;
using StereoKit.Framework;
using System;

namespace PassthroughDotNet
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Must be initialized BEFORE SK.Initialize is called
            //PassthroughFBExt passthroughStepper = SK.AddStepper<PassthroughFBExt>();
            PassthroughFBExt2 passthroughStepper2 = SK.AddStepper<PassthroughFBExt2>();

            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "FBSpatialEntity",
                assetsFolder = "Assets",
            };
            if (!SK.Initialize(settings))
                Environment.Exit(1);

            // Must be called AFTER SK.Initialize
            SK.AddStepper<Logger>();


            // Create assets used by the app
            Pose cubePose = new Pose(0, 0, -0.5f, Quat.Identity);
            Model cube = Model.FromMesh(
                Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                Default.MaterialUI);

            Matrix floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
            Material floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            Pose window1Pose = new Pose(-0.5f, 0, -0.3f, Quat.LookDir(1, 0, 1));
            Pose window2Pose = new Pose(0.2f, -0.1f, -0.3f, Quat.LookDir(-0.5f, 0, 1));

            bool isEnabled_XR_EXT_uuid          = Backend.OpenXR.ExtEnabled("XR_EXT_uuid");
            bool isEnabled_XR_FB_spatial_entity = Backend.OpenXR.ExtEnabled("XR_FB_spatial_entity");
            bool isExtEnabled_XR_FB_passthrough = Backend.OpenXR.ExtEnabled("XR_FB_passthrough");
            Log.Info("XR_EXT_uuid: "            + (isEnabled_XR_EXT_uuid ? "enabled" : "not enabled"));
            Log.Info("XR_FB_spatial_entity: "   + (isEnabled_XR_FB_spatial_entity ? "enabled" : "not enabled"));
            Log.Info("XR_FB_passthrough: "      + (isExtEnabled_XR_FB_passthrough ? "enabled" : "not enabled"));


            // Core application loop
            while (SK.Step(() =>
            {
                if (SK.System.displayType == Display.Opaque)
                    Default.MeshCube.Draw(floorMaterial, floorTransform);

                UI.Handle("Cube", ref cubePose, cube.Bounds);
                cube.Draw(cubePose.ToMatrix());

                // Passthrough menu
                //UI.WindowBegin("Passthrough Menu", ref windowPose);
                //if (passthroughStepper.Available)
                //{

                //    if (UI.Button("toggle"))
                //        passthroughStepper.EnabledPassthrough = !passthroughStepper.EnabledPassthrough;
                //    UI.Label($"Passthrough is {(passthroughStepper.EnabledPassthrough ? "ON" : "OFF")}");
                //}
                //else
                //{
                //    UI.Label("Passthrough is not available :(");
                //}
                //UI.WindowEnd();

                // Passthrough menu 2
                UI.WindowBegin("Passthrough Menu", ref window1Pose);
                if (passthroughStepper2.Available)
                {
                    if (UI.Button("toggle"))
                        passthroughStepper2.EnabledPassthrough = !passthroughStepper2.EnabledPassthrough;

                    UI.Label($"Passthrough is {(passthroughStepper2.EnabledPassthrough ? "ON" : "OFF")}");
                }
                else
                {
                    UI.Label("Passthrough is not available :(");
                }
                UI.WindowEnd();

                // Spatial Anchor Menu
                UI.WindowBegin("Spatial Anchor Menu", ref window2Pose);
                if (passthroughStepper2.SpatialAvailable)
                {
                    if (UI.Button("create-anchor"))
                        passthroughStepper2.CreateAnchor();
                }
                else
                {
                    UI.Label("Spatial Anchor is not available :(");
                }
                UI.WindowEnd();

            })) ;
            SK.Shutdown();
        }
    }
}
