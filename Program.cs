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
			PassthroughFBExt   passthroughStepper   = SK.AddStepper<PassthroughFBExt>();
			SpatialEntityFBExt spatialEntityStepper = SK.AddStepper<SpatialEntityFBExt>();

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


			// Core application loop
			while (SK.Step(() =>
			{
				if (SK.System.displayType == Display.Opaque)
					Default.MeshCube.Draw(floorMaterial, floorTransform);

				UI.Handle("Cube", ref cubePose, cube.Bounds);
				cube.Draw(cubePose.ToMatrix());

				// Passthrough menu
				UI.WindowBegin("Passthrough Menu", ref window1Pose);
				if (passthroughStepper.Available)
				{

					if (UI.Button("toggle"))
						passthroughStepper.EnabledPassthrough = !passthroughStepper.EnabledPassthrough;
					UI.Label($"Passthrough is {(passthroughStepper.EnabledPassthrough ? "ON" : "OFF")}");
				}
				else
				{
					UI.Label("Passthrough is not available :(");
				}
				UI.WindowEnd();

				// Spatial Anchor Menu
				UI.WindowBegin("Spatial Anchor Menu", ref window2Pose);
				if (spatialEntityStepper.Available)
				{
					UI.Label("FB Spatial Entity EXT available!");
					if (UI.Button("Create Anchor"))
					{
						// Create an anchor at pose of the right index finger tip
						Pose fingerPose = Input.Hand(Handed.Right)[FingerId.Index, JointId.Tip].Pose;
						spatialEntityStepper.CreateAnchor(fingerPose);
					}
				}
				else
				{
					UI.Label("Spatial Anchor is not available :(");
				}
				UI.WindowEnd();

				// Spatial anchor visual
				spatialEntityStepper.Anchors.ForEach(anchor =>
				{
					Mesh.Cube.Draw(Material.Default, anchor.pose.ToMatrix(0.2f), new Color(1, 0.5f, 0));
				});

			})) ;
			SK.Shutdown();
		}
	}
}
