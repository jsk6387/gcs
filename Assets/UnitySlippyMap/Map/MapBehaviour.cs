// 
//  Map.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
//  Copyright (c) 2012 Jonathan Derrough
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Collections;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Converters.WellKnownText;

using UnityEngine;

using UnitySlippyMap.Markers;
using UnitySlippyMap.Layers;
using UnitySlippyMap.UserGUI;
using UnitySlippyMap.MyInput;
using UnitySlippyMap.Helpers;
using UnityEngine.UI;
namespace UnitySlippyMap.Map
{
	/// <summary>
	/// The MapBehaviour class is a singleton handling layers and markers.
	/// Tiles are GameObjects (simple planes) parented to their layer's GameObject, in turn parented to the map's GameObject.
	/// Markers are empty GameObjects parented to the map's GameObject.
	/// The parenting is used to position the tiles and markers in a local referential using the map's center as origin.
	/// 
	/// Below is a basic example of how to create a map with a single OSM layer a few markers.
	/// </summary>
	/// <code>
	/// 
	///		using UnityEngine;
	///
	///		using System;
	///
	///		using UnitySlippyMap.Map;
	///		using UnitySlippyMap.Layers;
	///		using UnitySlippyMap.Markers;
	///
	/// 	public class TestMap : MonoBehaviour
	/// 	{
	///	 		private MapBehaviour map;
	///	
	///	 		public Texture	MarkerTexture;
	///	
	///	 		void Start()
	///	 		{
	///		 		// create the map singleton
	///		 		map = MapBehaviour.Instance;
	///		
	///				// 9 rue Gentil, Lyon, France
	///				map.CenterWGS84 = new double[2] { 4.83527, 45.76487 };
	///				map.UsesLocation = true;
	///				map.InputsEnabled = true;
	///
	///				// create a test layer
	///				TileLayerBehaviour layer = map.CreateLayer<OSMTileLayerBehaviour>("test tile layer");
	///				layer.URLFormat = "http://a.tile.openstreetmap.org/{0}/{1}/{2}.png";
	///
	///				// create some test 2D markers
	///				GameObject go = TileBehaviour.CreateTileTemplate();
	///				go.renderer.material.mainTexture = MarkerTexture;
	///				go.renderer.material.renderQueue = 4000;
	///
	///				GameObject markerGO;
	///				markerGO = Instantiate(go) as GameObject;
	///				map.CreateMarker<MarkerBehaviour>("test marker #1 - 9 rue Gentil, Lyon", new double[2] { 4.83527, 45.76487 }, markerGO);
	///		
	///				markerGO = Instantiate(go) as GameObject;
	///				map.CreateMarker<MarkerBehaviour>("test marker #2 - 31 rue de la Bourse, Lyon", new double[2] { 4.83699, 45.76535 }, markerGO);
	///		
	///				markerGO = Instantiate(go) as GameObject;
	///				map.CreateMarker<MarkerBehaviour>("test marker #3 - 1 place St Nizier, Lyon", new double[2] { 4.83295, 45.76468 }, markerGO);
	///
	///				DestroyImmediate(go);
	/// 
	///			}
	///	
	///			void OnApplicationQuit()
	///			{
	///				map = null;
	///			}
	///		}
	///
	/// </code>
	public class MapBehaviour : MonoBehaviour
	{
        #region Singleton stuff

        /// <summary>
        /// The instance.
        /// </summary>
        public Transform marker;
        public InputField fieldLabel;
        public Text textLabel;
		private static MapBehaviour instance = null;
        public double[] getWGS84 = new double[2];
        public double[] Altitude = new double[1];
        public int[] getWGS84toTile = new int[2];
        private ArrayList markerLat = new ArrayList();
        private ArrayList markerLong = new ArrayList();
        private ArrayList markerAlt = new ArrayList();
        private int markerCnt = 0;
        private int listCnt = 0;
        private float contentRowNum = 15.0f;
		/// <summary>
		/// Gets the instance.
		/// </summary>
		/// <value>The instance of the <see cref="UnitySlippyMap.Map.MapBehaviour"/> singleton.</value>
		public static MapBehaviour Instance {
			get {
				if (null == (object)instance) {
					instance = FindObjectOfType (typeof(MapBehaviour)) as MapBehaviour;
					if (null == (object)instance) {
						var go = new GameObject ("[Map]");
						//go.hideFlags = HideFlags.HideAndDontSave;
						instance = go.AddComponent<MapBehaviour> ();
						instance.EnsureMap ();
					}
				}

				return instance;
			}
		}
	    public float getContentRowNum()
        {
            return contentRowNum;
        }
	    
		/// <summary>
		/// Ensures the map.
		/// </summary>
		private void EnsureMap ()
		{
		}
	    
		/// <summary>
		/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.MapBehaviour"/> class.
		/// </summary>
		private MapBehaviour ()
		{
		}

		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		private void OnDestroy ()
		{
			instance = null;
		}

		/// <summary>
		/// Raises the application quit event.
		/// </summary>
		private void OnApplicationQuit ()
		{
			DestroyImmediate (this.gameObject);
		}
	    
	#endregion
	
	#region Variables & properties

		/// <summary>
		/// The current camera used to render the map.
		/// </summary>
		private Camera currentCamera;
		
		/// <summary>
		/// Gets or sets the current camera used to render the map.
		/// </summary>
		/// <value>The current camera used to render the map.</value>
		public Camera CurrentCamera {
			get { return currentCamera; }
			set { currentCamera = value; }
		}

		/// <summary>
		/// Indicates whether this instance is dirty and needs to be updated.
		/// </summary>
		private bool isDirty = false;

		/// <summary>
		/// Gets or sets a value indicating whether this instance is dirty and needs to be updated.
		/// </summary>
		/// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
		public bool IsDirty {
			get { return isDirty; }
			set { isDirty = value; }
		}

		/// <summary>
		/// The center coordinates of the map in the WGS84 coordinate system.
		/// </summary>
		private double[] centerWGS84 = new double[2];
        
		/// <summary>
		/// Gets or sets the center coordinates of the map in the WGS84 coordinate system.
		/// </summary>
		/// <value>
		/// When set, the map is refreshed and the <see cref="UnitySlippyMap.Map.CenterEPSG900913">center
		/// coordinates of the map in the EPSG 900913 coordinate system</see> are updated.
		/// </value>
		public double[] CenterWGS84 {
			get { return centerWGS84; }
			set {
				if (value == null) {
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.CenterWGS84: value cannot be null");
#endif
					return;
				}
				
				if (value [0] > 180.0)
					value [0] -= 360.0;
				else if (value [0] < -180.0)
					value [0] += 360.0;
				
				centerWGS84 = value;

				double[] newCenterESPG900913 = wgs84ToEPSG900913Transform.Transform (centerWGS84);

				centerEPSG900913 = ComputeCenterEPSG900913 (newCenterESPG900913);

				Debug.Log("center: " + centerEPSG900913[0] + " " + centerEPSG900913[1]);

				FitVerticalBorder ();
				IsDirty = true;
			}
		}
	
		/// <summary>
		/// The center coordinates in the EPSG 900913 coordinate system.
		/// </summary>
		private double[] centerEPSG900913 = new double[2];
	
		/// <summary>
		/// Gets or sets the center coordinates in the EPSG 900913 coordinate system.
		/// </summary>
		/// <value>When set, the map is refreshed and the center coordinates of the map in WGS84 are also updated.</value>
		public double[] CenterEPSG900913 {
			get {
				return centerEPSG900913;
			}
			set {
				if (value == null) {
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.CenterEPSG900913: value cannot be null");
#endif
					return;
				}

				centerEPSG900913 = ComputeCenterEPSG900913 (value);
				centerWGS84 = epsg900913ToWGS84Transform.Transform (centerEPSG900913);

				FitVerticalBorder ();
				IsDirty = true;
			}
		}
	
		// <summary>
		// Is used to constraint the map panning.
		// </summary>
		// TODO: implement the constraint
		//private double[]						size = new double[2];
	
		/// <summary>
		/// The current zoom.
		/// </summary>
		private float currentZoom;

		/// <summary>
		/// Gets or sets the current zoom.
		/// </summary>
		/// <value>When set, the map is refreshed.</value>
		public float CurrentZoom {
			get { return currentZoom; }
			set {
				if (value < minZoom
					|| value > maxZoom) {
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.Zoom: value must be inside range [" + minZoom + " - " + maxZoom + "]");
#endif
					return;
				}

				if (currentZoom == value)
					return;

				currentZoom = value;

				float diff = value - roundedZoom;
				if (diff > 0.0f && diff >= zoomStepLowerThreshold)
					roundedZoom = (int)Mathf.Ceil (currentZoom);
				else if (diff < 0.0f && diff <= -zoomStepUpperThreshold)
					roundedZoom = (int)Mathf.Floor (currentZoom);

				UpdateInternals ();

				FitVerticalBorder ();
			}
		}
	   
		/// <summary>
		/// The zoom step upper threshold.
		/// </summary>
		private float zoomStepUpperThreshold = 0.8f;

		/// <summary>
		/// Gets or sets the zoom step upper threshold.
		/// </summary>
		/// <value>The zoom step upper threshold determines if the zoom level of the map should change when zooming out.</value>
		public float ZoomStepUpperThreshold {
			get { return zoomStepUpperThreshold; }
			set { zoomStepUpperThreshold = value; }
		}
	
		/// <summary>
		/// The zoom step lower threshold.
		/// </summary>
		private float zoomStepLowerThreshold = 0.2f;

		/// <summary>
		/// Gets or sets the zoom step lower threshold.
		/// </summary>
		/// <value>The zoom step upper threshold determines if the zoom level of the map should change when zooming in.</value>
		public float ZoomStepLowerThreshold {
			get { return zoomStepLowerThreshold; }
			set { zoomStepLowerThreshold = value; }
		}
	
		/// <summary>
		/// The minimum zoom level for this map.
		/// </summary>
		private float minZoom = 3.0f;

		/// <summary>
		/// Gets or sets the minimum zoom.
		/// </summary>
		/// <value>
		/// This is the mininum zoom value for the map.
		/// Inferior zoom values are clamped when setting the <see cref="UnitySlippyMap.Map.CurrentZoom"/>.
		/// Additionally, values are always clamped between 3 and 18.
		/// </value>
		public float MinZoom {
			get { return minZoom; }
			set {
				if (value < 3.0f
					|| value > 18.0f) {
					minZoom = Mathf.Clamp (value, 3.0f, 18.0f);
				} else {		
					minZoom = value;
				}
			
				if (minZoom > maxZoom) {
#if DEBUG_LOG
				Debug.LogWarning("WARNING: Map.MinZoom: clamp value [" + minZoom + "] to max zoom [" + maxZoom + "]");
#endif
					minZoom = maxZoom;
				}
			}
		}
	
		/// <summary>
		/// The maximum zoom level for this map.
		/// </summary>
		private float maxZoom = 18.0f;

		/// <summary>
		/// Gets or sets the maximum zoom.
		/// </summary>
		/// <value>
		/// This is the maximum zoom value for the map.
		/// Superior zoom values are clamped when setting the <see cref="UnitySlippyMap.Map.CurrentZoom"/>.
		/// Additionally, values are always clamped between 3 and 18.
		/// </value>
		public float MaxZoom {
			get { return maxZoom; }
			set {
				if (value < 3.0f
					|| value > 18.0f) {
					maxZoom = Mathf.Clamp (value, 3.0f, 18.0f);
				} else {		
					maxZoom = value;
				}
			
				if (maxZoom < minZoom) {
#if DEBUG_LOG
				Debug.LogWarning("WARNING: Map.MaxZoom: clamp value [" + maxZoom + "] to min zoom [" + minZoom + "]");
#endif
					maxZoom = minZoom;
				}
			}
		}

		/// <summary>
		/// The rounded zoom.
		/// </summary>
		/// <value>It is updated when <see cref="UnitySlippyMap.Map.CurrentZoom"/> is set.</value>
		private int roundedZoom;

		/// <summary>
		/// Gets the rounded zoom.
		/// </summary>
		/// <value>The rounded zoom is updated when <see cref="UnitySlippyMap.Map.CurrentZoom"/> is set.</value>
		public int RoundedZoom { get { return roundedZoom; } }
	
		/// <summary>
		/// The half map scale.
		/// </summary>
		/// <value>
		/// It is used throughout the implementation to rule the camera elevation
		/// and the size/scale of the tiles.
		/// </value>
		private float halfMapScale = 0.0f;

		/// <summary>
		/// Gets the half map scale.
		/// </summary>
		/// <value>
		/// The half map scale is a value used throughout the implementation to rule the camera elevation
		/// and the size/scale of the tiles.
		/// </value>
		public float HalfMapScale { get { return halfMapScale; } }
	
		/// <summary>
		/// The rounded half map scale.
		/// </summary>
		private float roundedHalfMapScale = 0.0f;

		/// <summary>
		/// Gets the rounded half map scale.
		/// </summary>
		/// <value>See <see cref="UnitySlippyMap.Map.HalfMapScale"/> .</value>
		public float RoundedHalfMapScale { get { return roundedHalfMapScale; } }
	
		/// <summary>
		/// The number of meters per pixel in respect to the latitude and zoom level of the map.
		/// </summary>
		private float metersPerPixel = 0.0f;

		/// <summary>
		/// Gets the meters per pixel.
		/// </summary>
		/// <value>The number of meters per pixel in respect to the latitude and zoom level of the map.</value>
		public float MetersPerPixel { get { return metersPerPixel; } }

		/// <summary>
		/// The rounded meters per pixel.
		/// </summary>
		private float roundedMetersPerPixel = 0.0f;

		/// <summary>
		/// Gets the rounded meters per pixel.
		/// </summary>
		/// <value>See <see cref="UnitySlippyMap.Map.MetersPerPixel"/>.</value>
		public float RoundedMetersPerPixel { get { return roundedMetersPerPixel; } }

		/// <summary>
		/// The scale multiplier.
		/// </summary>
		/// <value>It helps converting meters (EPSG 900913) to Unity3D world coordinates.</value>
		private float scaleMultiplier = 0.0f;

		/// <summary>
		/// Gets the scale multiplier.
		/// </summary>
		/// <value>The scale multiplier helps converting meters (EPSG 900913) to Unity3D world coordinates.</value>
		public float ScaleMultiplier { get { return scaleMultiplier; } }

		/// <summary>
		/// The rounded scale multiplier.
		/// </summary>
		private float roundedScaleMultiplier = 0.0f;

		/// <summary>
		/// Gets the rounded scale multiplier.
		/// </summary>
		/// <value>See <see cref="UnitySlippyMap.Map.ScaleMultiplier"/>.</value>
		public float RoundedScaleMultiplier { get { return roundedScaleMultiplier; } }

		/// <summary>
		/// The scale divider.
		/// </summary>
		/// <value>
		/// It is an arbitrary value used to keep values within single floating point range when converting coordinates
		/// to Unity3D world coordinates.</value>
		private float scaleDivider = 20000.0f;

		/// <summary>
		/// The tile resolution.
		/// </summary>
		private float tileResolution = 256.0f;

		/// <summary>
		/// Gets the tile resolution.
		/// </summary>
		/// <value>The tile resolution in pixels.</value>
		public float TileResolution { get { return tileResolution; } }

		/// <summary>
		/// The screen scale.
		/// </summary>
		private float screenScale = 1.0f;

		/// <summary>
		/// The "uses location" flag.
		/// </summary>
		/// <value>It indicates whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> uses the host's location.</value>
		private bool usesLocation = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> uses the host's location.
		/// </summary>
		/// <value><c>true</c> if uses location; otherwise, <c>false</c>.</value>
		public bool UsesLocation {
			get { return usesLocation; }
			set {
				if (usesLocation == value)
					return;
			
				usesLocation = value;
			
				if (usesLocation) {
					if (UnityEngine.Input.location.isEnabledByUser
						&& (UnityEngine.Input.location.status == LocationServiceStatus.Stopped
						|| UnityEngine.Input.location.status == LocationServiceStatus.Failed)) {
						UnityEngine.Input.location.Start ();
					} else {
#if DEBUG_LOG
					Debug.LogError("ERROR: Map.UseLocation: Location is not authorized on the device.");
#endif
					}
				} else {
					if (UnityEngine.Input.location.isEnabledByUser
						&& (UnityEngine.Input.location.status == LocationServiceStatus.Initializing
						|| UnityEngine.Input.location.status == LocationServiceStatus.Running)) {
						UnityEngine.Input.location.Start ();
					}
				}
			}
		}
	
		/// <summary>
		/// The "updates center with location" flag.
		/// </summary>
		private bool updatesCenterWithLocation = true;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> updates its center with the host's location.
		/// </summary>
		/// <value>
		/// <c>true</c> if update center with location; otherwise, <c>false</c>.
		/// It is automatically set to <c>false</c> when the map is manipulated by the user.
		/// </value>
		public bool UpdatesCenterWithLocation {
			get {
				return updatesCenterWithLocation;
			}
		
			set {
				updatesCenterWithLocation = value;
			}
		}
	
		/// <summary>
		/// The "uses orientation" flag.
		/// </summary>
		private bool usesOrientation = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> uses the host's orientation.
		/// </summary>
		/// <value><c>true</c> if use orientation; otherwise, <c>false</c>.</value>
		public bool UsesOrientation {
			get { return usesOrientation; }
			set {
				if (usesOrientation == value)
					return;
			
				usesOrientation = value;
			
				if (usesOrientation) {
					// http://docs.unity3d.com/Documentation/ScriptReference/Compass-enabled.html
					// Note, that if you want Input.compass.trueHeading property to contain a valid value,
					// you must also enable location updates by calling Input.location.Start().
					if (usesLocation == false) {
						if (UnityEngine.Input.location.isEnabledByUser
							&& (UnityEngine.Input.location.status == LocationServiceStatus.Stopped
							|| UnityEngine.Input.location.status == LocationServiceStatus.Failed)) {
							UnityEngine.Input.location.Start ();
						} else {
#if DEBUG_LOG
						Debug.LogError("ERROR: Map.UseOrientation: Location is not authorized on the device.");
#endif
						}
					}
					UnityEngine.Input.compass.enabled = true;
				} else {
					if (usesLocation == false) {
						if (UnityEngine.Input.location.isEnabledByUser
							&& (UnityEngine.Input.location.status == LocationServiceStatus.Initializing
							|| UnityEngine.Input.location.status == LocationServiceStatus.Running))
							UnityEngine.Input.location.Start ();
					}
					UnityEngine.Input.compass.enabled = false;
				}
			}
		}
	
		/// <summary>
		/// The "camera follows orientation" flag.
		/// </summary>
		private bool cameraFollowsOrientation = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/>'s camera follows the host's orientation.
		/// </summary>
		/// <value>
		/// <c>true</c> if the camera follows the host's orientation; otherwise, <c>false</c>.
		/// If set to <c>true</c>, <see cref="UnitySlippyMap.Map.CameraFollowsOrientation"/> is set to <c>true</c>.
		/// </value>
		public bool CameraFollowsOrientation {
			get { return cameraFollowsOrientation; }
			set {
				cameraFollowsOrientation = value;
				lastCameraOrientation = 0.0f;
			}
		}
	
		/// <summary>
		/// The last camera orientation.
		/// </summary>
		private float lastCameraOrientation = 0.0f;
        
		/// <summary>
		/// The "shows GUI controls" flag.
		/// </summary>
		private bool showsGUIControls = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> shows GUI controls.
		/// </summary>
		/// <value><c>true</c> if show GUI controls; otherwise, <c>false</c>.</value>
		public bool ShowsGUIControls
		{
			get { return showsGUIControls; }
			set { showsGUIControls = value; }
		}

		/// <summary>
		/// The "inputs enabled" flag.
		/// </summary>
		private bool inputsEnabled = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> inputs are enabled.
		/// </summary>
		/// <value>
		/// <c>true</c> if inputs enabled; otherwise, <c>false</c>.
		/// TODO: implement inputs in a user oriented customizable way
		/// </value>
		public bool InputsEnabled
		{
			get { return inputsEnabled; }
			set { inputsEnabled = value; }
		}
        
		/// <summary>
		/// The list of <see cref="UnitySlippyMap.Layer"/> instances.
		/// </summary>
		private List<LayerBehaviour> layers = new List<LayerBehaviour> ();
	
		/// <summary>
		/// The "has moved" flag.
		/// </summary>
		private bool hasMoved = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> has moved.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance has moved; otherwise, <c>false</c>.
		/// The map will not update when it is true and will set it to false at the end of its Update.
		/// </value>
		public bool HasMoved {
			get { return hasMoved; }
			set { hasMoved = value; }
		}
    
		/// <summary>
		/// The GUI delegate.
		/// </summary>
		private GUIDelegate guiDelegate;

		/// <summary>
		/// Gets or sets the GUI delegate.
		/// </summary>
		/// <value>The GUI delegate.</value>
		public GUIDelegate GUIDelegate {
			get { return guiDelegate; }
			set { guiDelegate = value; }
		}
	
		/// <summary>
		/// The input delegate.
		/// </summary>
		private InputDelegate inputDelegate;

		/// <summary>
		/// Gets or sets the input delegate.
		/// </summary>
		/// <value>The input delegate.</value>
		public InputDelegate InputDelegate {
			get { return inputDelegate; }
			set { inputDelegate = value; }
		}
	
		/// <summary>
		/// The "was input intercepted by GUI" flag.
		/// </summary>
		private bool wasInputInterceptedByGUI;
	
	
	
		/// <summary>
		/// The Well-Known Text representation of the EPSG900913 projection.
		/// </summary>
		// <value>ProjNet Dll: http://projnet.codeplex.com/</value>
		private static string wktEPSG900913 =
        "PROJCS[\"WGS84 / Simple Mercator\", " +
			"GEOGCS[\"WGS 84\", " +
			"DATUM[\"World Geodetic System 1984\", SPHEROID[\"WGS 84\", 6378137.0, 298.257223563,AUTHORITY[\"EPSG\",\"7030\"]], " +
			"AUTHORITY[\"EPSG\",\"6326\"]]," +
			"PRIMEM[\"Greenwich\", 0.0, AUTHORITY[\"EPSG\",\"8901\"]], " +
			"UNIT[\"degree\",0.017453292519943295], " +
			"AXIS[\"Longitude\", EAST], AXIS[\"Latitude\", NORTH]," +
			"AUTHORITY[\"EPSG\",\"4326\"]], " +
			"PROJECTION[\"Mercator_1SP\"]," +
			"PARAMETER[\"semi_minor\", 6378137.0], " +
			"PARAMETER[\"latitude_of_origin\",0.0], " +
			"PARAMETER[\"central_meridian\", 0.0], " +
			"PARAMETER[\"scale_factor\",1.0], " +
			"PARAMETER[\"false_easting\", 0.0], " +
			"PARAMETER[\"false_northing\", 0.0]," +
			"UNIT[\"m\", 1.0], " +
			"AXIS[\"x\", EAST], AXIS[\"y\", NORTH]," +
			"AUTHORITY[\"EPSG\",\"900913\"]]";

		/// <summary>
		/// Gets the Well-Known Text representation of the EPSG900913 projection.
		/// </summary>
		public static string WKTEPSG900913 { get { return wktEPSG900913; } }

		/// <summary>
		/// The CoordinateTransformationFactory instance.
		/// </summary>
		private CoordinateTransformationFactory ctFactory;

		/// <summary>
		/// Gets the CoordinateTransformationFactory instance.
		/// </summary>
		public CoordinateTransformationFactory CTFactory { get { return ctFactory; } }

		/// <summary>
		/// The EPSG 900913 ICoordinateSystem instance.
		/// </summary>
		private ICoordinateSystem epsg900913;

		/// <summary>
		/// Gets the EPSG 900913 ICoordinateSystem instance.
		/// </summary>
		public ICoordinateSystem EPSG900913 { get { return epsg900913; } }

		/// <summary>
		/// The WGS84 to EPSG 900913 ICoordinateTransformation instance.
		/// </summary>
		private ICoordinateTransformation wgs84ToEPSG900913;

		/// <summary>
		/// Gets the WGS84 to EPSG 900913 ICoordinateTransformation instance.
		/// </summary>
		public ICoordinateTransformation WGS84ToEPSG900913 { get { return wgs84ToEPSG900913; } }

		/// <summary>
		/// The WGS84 to EPSG 900913 IMathTransform instance.
		/// </summary>
		private IMathTransform wgs84ToEPSG900913Transform;

		/// <summary>
		/// Gets the WGS84 to EPSG900913 IMathTransform instance.
		/// </summary>
		public IMathTransform WGS84ToEPSG900913Transform { get { return wgs84ToEPSG900913Transform; } }

		/// <summary>
		/// The EPSG 900913 to WGS84 IMathTransform instance.
		/// </summary>
		private IMathTransform epsg900913ToWGS84Transform;

		/// <summary>
		/// Gets the EPSG 900913 to WGS84 IMathTransform instance.
		/// </summary>
		public IMathTransform EPSG900913ToWGS84Transform { get { return epsg900913ToWGS84Transform; } }
	
	#endregion
    
    #region Private methods
    
		/// <summary>
		/// Fits the vertical border.
		/// </summary>
		private void FitVerticalBorder ()
		{
			//TODO: take into account the camera orientation

			if (currentCamera != null) {
				double[] camCenter = new double[] {
										centerEPSG900913 [0],
										centerEPSG900913 [1]
								};
				double offset = Mathf.Floor (currentCamera.pixelHeight * 0.5f) * metersPerPixel;
				if (camCenter [1] + offset > GeoHelpers.HalfEarthCircumference) {
					camCenter [1] -= camCenter [1] + offset - GeoHelpers.HalfEarthCircumference;
					CenterEPSG900913 = camCenter;
				} else if (camCenter [1] - offset < -GeoHelpers.HalfEarthCircumference) {
					camCenter [1] -= camCenter [1] - offset + GeoHelpers.HalfEarthCircumference;
					CenterEPSG900913 = camCenter;
				}
			}
		}

		/// <summary>
		/// Computes the center EPS g900913.
		/// </summary>
		/// <returns>The center coordinate in the EPSG 900913 coordinate system.</returns>
		/// <param name="pos">Position.</param>
		private double[] ComputeCenterEPSG900913 (double[] pos)
		{
			Vector3 displacement = new Vector3 ((float)(centerEPSG900913 [0] - pos [0]) * roundedScaleMultiplier, 0.0f, (float)(centerEPSG900913 [1] - pos [1]) * roundedScaleMultiplier);
			Vector3 rootPosition = this.gameObject.transform.position;
			this.gameObject.transform.position = new Vector3 (
			rootPosition.x + displacement.x,
			rootPosition.y + displacement.y,
			rootPosition.z + displacement.z);

			if (pos [0] > GeoHelpers.HalfEarthCircumference)
				pos [0] -= GeoHelpers.EarthCircumference;
			else if (pos [0] < -GeoHelpers.HalfEarthCircumference)
				pos [0] += GeoHelpers.EarthCircumference;
            
            return pos;
		}

		/// <summary>
		/// Updates the internals of the <see cref="UnitySlippyMap.Map.MapBehaviour"/> instance.
		/// </summary>
		private void UpdateInternals ()
		{
			// FIXME: the half map scale is a value used throughout the implementation to rule the camera elevation
			// and the size/scale of the tiles, it depends on fixed tile size and resolution (here 256 and 72) so I am not
			// sure it would work for a tile layer with different values...
			// maybe there is a way to take the values out of the calculations and reintroduce them on Layer level...
			// FIXME: the 'division by 20000' helps the values to be kept in range for the Unity3D engine, not sure
			// this is the right approach either, feels kinda voodooish...
		
			halfMapScale = GeoHelpers.OsmZoomLevelToMapScale (currentZoom, 0.0f, tileResolution, 72) / scaleDivider;
			roundedHalfMapScale = GeoHelpers.OsmZoomLevelToMapScale (roundedZoom, 0.0f, tileResolution, 72) / scaleDivider;

			metersPerPixel = GeoHelpers.MetersPerPixel (0.0f, (float)currentZoom);
			roundedMetersPerPixel = GeoHelpers.MetersPerPixel (0.0f, (float)roundedZoom);
        
			// FIXME: another voodoish value to help converting meters (EPSG 900913) to Unity3D world coordinates
			scaleMultiplier = halfMapScale / (metersPerPixel * tileResolution);
			roundedScaleMultiplier = roundedHalfMapScale / (roundedMetersPerPixel * tileResolution);
		}
    
    #endregion
	
	#region MonoBehaviour implementation
	
		/// <summary>
		/// Raises the Awake event.
		/// </summary>
		private void Awake ()
		{
			// initialize the coordinate transformation
			epsg900913 = CoordinateSystemWktReader.Parse (wktEPSG900913) as ICoordinateSystem;
			ctFactory = new CoordinateTransformationFactory ();
			wgs84ToEPSG900913 = ctFactory.CreateFromCoordinateSystems (GeographicCoordinateSystem.WGS84, epsg900913);
			wgs84ToEPSG900913Transform = wgs84ToEPSG900913.MathTransform;
			epsg900913ToWGS84Transform = wgs84ToEPSG900913Transform.Inverse ();
		}
	
		/// <summary>
		/// Raises the Start event.
		/// </summary>
		private void Start ()
		{
			// setup the gui scale according to the screen resolution
			if (Application.platform == RuntimePlatform.Android
				|| Application.platform == RuntimePlatform.IPhonePlayer)
				screenScale = (Screen.orientation == ScreenOrientation.Landscape ? Screen.width : Screen.height) / 480.0f;
			else
				screenScale = 2.0f;

			// initialize the camera position and rotation
			currentCamera.transform.rotation = Quaternion.Euler (90.0f, 0.0f, 0.0f);
			Zoom (0.0f);
		}

		/// <summary>
		/// Raises the GUI event.
		/// </summary>
		private void OnGUI ()
		{
			// FIXME: gaps beween tiles appear when zooming and panning the map at the same time on iOS, precision ???
			// TODO: optimise, use one mesh for the tiles and combine textures in a big one (might resolve the gap bug above)

			// process the user defined GUI
			if (ShowsGUIControls && guiDelegate != null) {
				wasInputInterceptedByGUI = guiDelegate (this);
			}
		
			if (Event.current.type != EventType.Repaint
				&& Event.current.type != EventType.MouseDown
				&& Event.current.type != EventType.MouseDrag
				&& Event.current.type != EventType.MouseMove
				&& Event.current.type != EventType.MouseUp)
				return;
		
			if (InputsEnabled && inputDelegate != null) {
				inputDelegate (this, wasInputInterceptedByGUI);
			}
		
		}

		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Update().
		/// During an update cycle:
		/// * The map may use the host's location to update its center (see <see cref="UnitySlippyMap.Map.MapBehaviour.UsesLocation"/>
		/// for more information).
		/// * The camera may be rotated to follow the host's orientation (see <see cref="UnitySlippyMap.Map.MapBehaviour.UsesOrientation"/>
		/// for more information).
		/// * If the map has moved (see <see cref="UnitySlippyMap.Map.MapBehaviour.HasMoved"/> for more information) since the last update cycle,
		/// all tile downloading jobs are paused temporarily until the next update cycle when the map won't have moved.
		/// * If the map has moved (see <see cref="UnitySlippyMap.Map.MapBehaviour.HasMoved"/> for more information) and is dirty
		/// (see <see cref="UnitySlippyMap.Map.MapBehaviour.IsDirty"/> for more information), all markers and layers are updated
		/// and the GameObject supporting the map behaviour is repositioned to the center of the world (Vector3.Zero).
		/// </summary>
		private void Update ()
		{
#if DEBUG_PROFILE
		UnitySlippyMap.Profiler.Begin("Map.Update");
#endif
		
			// update the centerWGS84 with the last location if enabled
			if (usesLocation
				&& UnityEngine.Input.location.status == LocationServiceStatus.Running) {
				if (updatesCenterWithLocation) {
					if (UnityEngine.Input.location.lastData.longitude <= 180.0f
						&& UnityEngine.Input.location.lastData.longitude >= -180.0f
						&& UnityEngine.Input.location.lastData.latitude <= 90.0f
						&& UnityEngine.Input.location.lastData.latitude >= -90.0f) {
						if (CenterWGS84 [0] != UnityEngine.Input.location.lastData.longitude
							|| CenterWGS84 [1] != UnityEngine.Input.location.lastData.latitude)
							CenterWGS84 = new double[2] {
																UnityEngine.Input.location.lastData.longitude,
																UnityEngine.Input.location.lastData.latitude
														};
					
						//Debug.Log("DEBUG: Map.Update: new location: " + Input.location.lastData.longitude + " " + Input.location.lastData.latitude + ":  " + Input.location.status);					
					} else {
						Debug.LogWarning ("WARNING: Map.Update: bogus location (bailing): " + UnityEngine.Input.location.lastData.longitude + " " + UnityEngine.Input.location.lastData.latitude + ":  " + UnityEngine.Input.location.status);
					}
				}
			
				
			}
		
			// update the orientation of the location marker
			if (usesOrientation) {
				float heading = 0.0f;
				// TODO: handle all device orientations
				switch (Screen.orientation) {
				case ScreenOrientation.LandscapeLeft:
					heading = UnityEngine.Input.compass.trueHeading;
					break;
				case ScreenOrientation.Portrait: // FIXME: not tested, likely wrong, legacy code
					heading = -UnityEngine.Input.compass.trueHeading;
					break;
				}

				if (cameraFollowsOrientation) {
					if (lastCameraOrientation == 0.0f) {
						currentCamera.transform.RotateAround (Vector3.zero, Vector3.up, heading);

						lastCameraOrientation = heading;
					} else {
						float cameraRotationSpeed = 1.0f;
						float relativeAngle = (heading - lastCameraOrientation) * cameraRotationSpeed * Time.deltaTime;
						if (relativeAngle > 0.01f) {
							currentCamera.transform.RotateAround (Vector3.zero, Vector3.up, relativeAngle);
	
							//Debug.Log("DEBUG: cam: " + lastCameraOrientation + ", heading: " + heading +  ", rel angle: " + relativeAngle);
							lastCameraOrientation += relativeAngle;
						} else {
							currentCamera.transform.RotateAround (Vector3.zero, Vector3.up, heading - lastCameraOrientation);
	
							//Debug.Log("DEBUG: cam: " + lastCameraOrientation + ", heading: " + heading +  ", rel angle: " + relativeAngle);
							lastCameraOrientation = heading;
						}
					}
					
					IsDirty = true;
				}
			}
		
			// pause the loading operations when moving
			if (hasMoved == true) {
				TileDownloaderBehaviour.Instance.PauseAll ();
			} else {
				TileDownloaderBehaviour.Instance.UnpauseAll ();
			}
			
			// update the tiles if needed
			if (IsDirty == true && hasMoved == false) {
#if DEBUG_LOG
			Debug.Log("DEBUG: Map.Update: update layers & markers");
#endif
			
				IsDirty = false;
			
				foreach (LayerBehaviour layer in layers) {	
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				if (layer.gameObject.active == true
#else
					if (layer.gameObject.activeSelf == true
#endif
						&& layer.enabled == true
						&& CurrentZoom >= layer.MinZoom
						&& CurrentZoom <= layer.MaxZoom)
						layer.UpdateContent ();
				}
			
				if (this.gameObject.transform.position != Vector3.zero)
					this.gameObject.transform.position = Vector3.zero;

#if DEBUG_LOG
			Debug.Log("DEBUG: Map.Update: updated layers");
#endif
			}
		
			// reset the deferred update flag
			hasMoved = false;
						
#if DEBUG_PROFILE
		UnitySlippyMap.Profiler.End("Map.Update");
#endif
            if(UnityEngine.Input.GetMouseButtonDown(1))
            {
                //translate screen position to GWS84 by using RaycastHitToWGS84
                Ray ray=Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                RaycastHit hit;
                if(Physics.Raycast(ray,out hit))
                getWGS84= GeoHelpers.RaycastHitToWGS84(this, hit);
                drawMarker(hit);
                saveMarker(getWGS84);
                drawGPSInfo(getWGS84);
            }
		}
	    public void drawMarker(RaycastHit hit)
        {
            Transform newMarker = Instantiate(marker);
            Vector3 vecMarker = new Vector3(0, 0, 0);
            //double[] RaycastHitToEPSG900913 = new double[] { CenterEPSG900913[0] + hit.point.x,CenterEPSG900913[1] + hit.point.z };
            //draw marker
            vecMarker[0] = (float)hit.point.x;
            vecMarker[2] = (float)hit.point.z;
            newMarker.position = vecMarker;
        }
        /// <summary>
        /// draw Marker on the Map
        /// </summary>
        /// <param name="pos"></param>
        public void drawMarker(double[] pos)
        {
            Transform newMarker = Instantiate(marker);
            Vector3 vecMarker = new Vector3(0, 0, 0);
            pos = GeoHelpers.WGS84ToRaycastHit(this, pos);
            vecMarker[0] = (float)pos[0];   
            vecMarker[2] = (float)pos[1];
            newMarker.position = vecMarker;
        }
        public void drawMarker(PositionDouble pos)
        {
            double[] posArr=new double[2];
            posArr[0] = pos.longitude;
            posArr[1] = pos.latitude;
            Transform newMarker = Instantiate(marker);
            Vector3 vecMarker = new Vector3(0, 0, 0);
            posArr = GeoHelpers.WGS84ToRaycastHit(this, posArr);
            vecMarker[0] = (float)posArr[0];
            vecMarker[2] = (float)posArr[1];
            newMarker.position = vecMarker;
            saveMarker(posArr);
            markerAlt[markerAlt.LastIndexOf(100)] = pos.altitude;
        }
        /// <summary>
        /// draw Marker GPS information in the List
        /// </summary>
        public void drawGPSInfo(double[] pos)
        {
            InputField newFieldLong = Instantiate(fieldLabel);
            RectTransform Content = GameObject.Find("Content").GetComponent<RectTransform>();
            newFieldLong.GetComponent<RectTransform>().position = new Vector3(140, -17 - markerLat.IndexOf(pos[1]) * contentRowNum, 0);

            newFieldLong.text = pos[0].ToString();
            newFieldLong.transform.SetParent(Content.transform, false);

            InputField newFieldLat = Instantiate(fieldLabel);
            newFieldLat.GetComponent<RectTransform>().position = new Vector3(240, -17 - markerLat.IndexOf(pos[1]) * contentRowNum, 0);
            newFieldLat.text = pos[1].ToString();
            newFieldLat.transform.SetParent(Content.transform, false);
            
            InputField newFieldAlt = Instantiate(fieldLabel);
            newFieldAlt.GetComponent<RectTransform>().position = new Vector3(340, -17 - markerLat.IndexOf(pos[1]) * contentRowNum, 0);
            newFieldAlt.text = Altitude[0].ToString();
            newFieldAlt.transform.SetParent(Content.transform, false);

            Text fieldNum = Instantiate(textLabel);
            fieldNum.GetComponent<RectTransform>().position = new Vector3(39, -27 - markerLat.IndexOf(pos[1]) * contentRowNum, 0);
            fieldNum.text = "" + (markerLat.IndexOf(pos[1])+1);
            fieldNum.transform.SetParent(Content.transform, false);
           // markerCnt=markerLat.Count;
        }
        public void drawGPSInfo(PositionDouble pos)
        {
            InputField newFieldLong = Instantiate(fieldLabel);
            RectTransform Content = GameObject.Find("Content").GetComponent<RectTransform>();
            newFieldLong.GetComponent<RectTransform>().position = new Vector3(140, -17 - listCnt * contentRowNum, 0);

            newFieldLong.text = pos.longitude.ToString();
            newFieldLong.transform.SetParent(Content.transform, false);

            InputField newFieldLat = Instantiate(fieldLabel);
            newFieldLat.GetComponent<RectTransform>().position = new Vector3(240, -17 - listCnt * contentRowNum, 0);
            newFieldLat.text = pos.latitude.ToString();
            newFieldLat.transform.SetParent(Content.transform, false);

            InputField newFieldAlt = Instantiate(fieldLabel);
            newFieldAlt.GetComponent<RectTransform>().position = new Vector3(340, -17 - listCnt * contentRowNum, 0);
            newFieldAlt.text = pos.altitude.ToString();
            newFieldAlt.transform.SetParent(Content.transform, false);

            Text fieldNum = Instantiate(textLabel);
            fieldNum.GetComponent<RectTransform>().position = new Vector3(39, -27 - listCnt * contentRowNum, 0);
            fieldNum.text = "" + (listCnt + 1);
            fieldNum.transform.SetParent(Content.transform, false);
            listCnt++;
        }

        public int getMarkerCnt() { return markerLat.Count; }
        public double getMarkerLat(int i) { return (double) markerLat[i]; }
        public ArrayList getMarkerLat() { return markerLat; }
        public double getMarkerLong(int i) { return (double) markerLong[i]; }
        public ArrayList getMarkerLong() { return markerLong; }
        public double getMarkerAlt(int i) { return (double)markerAlt[i]; }
        public ArrayList getMarkerAlt() { return markerAlt; }
        public void setListCnt(int set) { listCnt = set; }
        /// <summary>
        /// Save Marker Latitude and Longtitude
        /// </summary>
        public void saveMarker(double[] pos)
        {
            pos[0] = Math.Round(pos[0], 6, MidpointRounding.AwayFromZero);
            pos[1] = Math.Round(pos[1], 6, MidpointRounding.AwayFromZero);
            markerLong.Add(pos[0]);
            markerLat.Add(pos[1]);
            markerAlt.Add(100);
        }
        #endregion

        #region MapBehaviour methods
        


		/// <summary>
		/// Creates a new named layer.
		/// </summary>
		/// <returns>The layer.</returns>
		/// <param name="name">The layer's name.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T CreateLayer<T> (string name) where T : LayerBehaviour
		{
			// create a GameObject as the root of the layer and add the templated Layer component to it
			GameObject layerRoot = new GameObject (name);
			Transform layerRootTransform = layerRoot.transform;
			//Debug.Log("DEBUG: layer root: " + layerRootTransform.position + " this position: " + this.gameObject.transform.position);
			layerRootTransform.parent = this.gameObject.transform;
			layerRootTransform.localPosition = Vector3.zero;
			T layer = layerRoot.AddComponent<T> ();
		
			// setup the layer
			layer.Map = this;
			layer.MinZoom = minZoom;
			layer.MaxZoom = maxZoom;
		
			// add the layer to the layers' list
			layers.Add (layer);
		
			// tell the map to update
			IsDirty = true;
		
			return layer;
		}
        
    
		/// <summary>
		/// Zooms the map at the specified zoomSpeed.
		/// </summary>
		/// <param name="zoomSpeed">Zoom speed.</param>
		public void Zoom (float zoomSpeed)
		{
			// apply the zoom
			CurrentZoom += 4.0f * zoomSpeed * Time.deltaTime;

			// move the camera
			// FIXME: the camera jumps on the first zoom when tilted, because the cam altitude and zoom value are unsynced by the rotation
			Transform cameraTransform = currentCamera.transform;
			float y = GeoHelpers.OsmZoomLevelToMapScale (currentZoom, 0.0f, tileResolution, 72) / scaleDivider * screenScale;
			float t = y / cameraTransform.forward.y;
			cameraTransform.position = new Vector3 (
			t * cameraTransform.forward.x,
			y,
			t * cameraTransform.forward.z);
		
			// set the update flag to tell the behaviour the user is manipulating the map
			hasMoved = true;
			IsDirty = true;
		}
	
	#endregion
		
	}

}