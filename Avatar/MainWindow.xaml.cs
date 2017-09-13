using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.IO;
using System;
using System.Collections.Generic;
using System.Timers;
using NUClass;

namespace Avatar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string components_dir = "C:\\Users\\Nathan\\AppData\\Local/M-Mark/M-Mark/AvatarComponents/";
        //string input_data = @"C:\Users\Nathan\Dev\Imperial\IMU\IMU_visualisation\90_chest.csv";
        string input_data = @"C:\Users\Nathan\AppData\Local\M-Mark\M-Mark\ExerciseData\Patient - practice-3Reps - 1-Hold phone - Right-2017-09-09T13-21-46.csv";
        public List<Quaternion[]> initial_quaternions = new List<Quaternion[]>();
        private readonly Quaternion offset = new Quaternion(0, 0.7071067811865475, 0.7071067811865476, 0);
        private Quaternion ninetyx = new Quaternion(0,0.7071,    0.7071,         0);
        ConnectionManager cm = new ConnectionManager();

        /// <summary>
        /// The coordinates of the center of rotation of the torso
        /// </summary>
        private double[] torso_centroid = { 0.83, 4.63, 56.9 };
        private double[] right_UA_centroid = { 27.8, 4.89, 100 };
        private double[] right_FA_centroid = { 27.8, 4.89, 54.4 };
        private double[] right_hand_centroid = { 27.8, 4.89, 8.32 };
        private List<double[]> centroids = new List<double[]>();
        private Quaternion[] initial_frame = new Quaternion[4];



        /// <summary>
        /// The coordinates of the center of rotation of the Left upper arm (i.e. left shoulder)
        /// </summary>
        private const double LeftUpperarmCenterOfRotationX = -29.1, LeftUpperarmCenterOfRotationY = 4.89, LeftUpperarmCenterOfRotationZ = 100;

        /// <summary>
        /// The coordinates of the center of rotation of the Right upper arm (i.e right shoulder)
        /// </summary>
        private const double RightUpperarmCenterOfRotationX = 27.8, RightUpperarmCenterOfRotationY = 4.89, RightUpperarmCenterOfRotationZ = 100;

        /// <summary>
        /// The coordinates of the center of rotation of the Left Forearm (i.e. left elbow)
        /// </summary>
        private const double LeftForearmCenterOfRotationX = -29.1, LeftForearmCenterOfRotationY = 4.89, LeftForearmCenterOfRotationZ = 54.4;

        /// <summary>
        /// The coordinates of the center of rotation of the Right Forearm (i.e. right elbow)
        /// </summary>
        private const double RightForearmCenterOfRotationX = 27.8, RightForearmCenterOfRotationY = 4.89, RightForearmCenterOfRotationZ = 54.4;

        /// <summary>
        /// The coordinates of the center of rotation of the Left Hand (i.e. left wrist)
        /// </summary>
        private const double LeftHandCenterOfRotationX = -29.1, LeftHandCenterOfRotationY = 4.89, LeftHandCenterOfRotationZ = 8.32;

        /// <summary>
        /// The coordinates of the center of rotation of the Right Hand (i.e. right wrist)
        /// </summary>
        private const double RightHandCenterOfRotationX = 27.8, RightHandCenterOfRotationY = 4.89, RightHandCenterOfRotationZ = 8.32;
        /// <summary>
        /// The 3D model group containing the components for the model
        /// </summary>
        private Model3DGroup skeleton;

        /// <summary>
        /// Stores the Avatar's waist 3D model component
        /// </summary>
        private Model3D waist;

        /// <summary>
        /// Stores the Avatar's Torso 3D model component
        /// </summary>
        private Model3D torso;

        /// <summary>
        /// Stores the Avatar's left upper arm, forearm and hand 3D model component respectively 
        /// </summary>
        private Model3D leftUpperArm, leftForearm, leftHand;

        /// <summary>
        /// Stores the Avatar's right upper arm, forearm and hand 3D model component respectively 
        /// </summary>
        private Model3D rightUpperArm, rightForearm, rightHand;

        ProcessedDataReader pdr;
        ModelVisual3D modelVisual3D;
        private Timer aTimer;
        System.Windows.Threading.DispatcherTimer animationTimer;
        private int step = 1;
        public Model3D AvatarModel { get; set; }
        public Model3D[] components;
        public Quaternion[] orientations = new Quaternion[4];
        public Quaternion[] previous = new Quaternion[4];
        int[] count = {1,1,1,1};
        int[] ready = { 0, 0, 0, 0 };


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            centroids.Add(torso_centroid);
            centroids.Add(right_UA_centroid);
            centroids.Add(right_FA_centroid);
            centroids.Add(right_hand_centroid);
            orientations[0] = Quaternion.Identity;
            orientations[1] = Quaternion.Identity;
            orientations[2] = Quaternion.Identity;
            orientations[3] = Quaternion.Identity;
            previous[0] = Quaternion.Identity;
            previous[1] = Quaternion.Identity;
            previous[2] = Quaternion.Identity;
            previous[3] = Quaternion.Identity;
            initial_frame[0] = new Quaternion(-0.330925973595304, -0.29698484809835, 0.524673231640418, 0.725491557497398);
            initial_frame[1] = new Quaternion(- 0.323147799002252,0.173241161390704,0.143542676580869,-0.918531708761325);
            initial_frame[2] = new Quaternion(0.580534667354156, -0.00565685424949242, 0.410121933088198, -0.703571247280615);
            initial_frame[3] = new Quaternion(0.399515331370399, -0.20081832585698, 0.526087445202791, -0.723370237153838);
            System.Diagnostics.Debug.WriteLine("FIRED");
            //The Importer to load .obj files
            ModelImporter importer = new ModelImporter();

            //The Material (Color) that is applyed to the importet objects
            Material material = new DiffuseMaterial(new SolidColorBrush(Colors.BurlyWood));
            importer.DefaultMaterial = material;

            //instanciate a new group of 3D Models
            skeleton = new Model3DGroup();

            waist = importer.Load(Path.Combine(components_dir, "waist.stl"));
            torso = importer.Load(Path.Combine(components_dir, "rightTorso.stl"));
            rightUpperArm = importer.Load(Path.Combine(components_dir, "rightUpperArm.stl"));
            rightForearm = importer.Load(Path.Combine(components_dir, "rightForeArm.stl"));
            rightHand = importer.Load(Path.Combine(components_dir, "rightHand.stl"));

            skeleton.Children.Add(waist);
            skeleton.Children.Add(torso);
            skeleton.Children.Add(rightUpperArm);
            skeleton.Children.Add(rightForearm);
            skeleton.Children.Add(rightHand);

            components = new Model3D[] { torso, rightUpperArm, rightForearm, rightHand};

            var componentTransform = new Transform3DGroup();

            RotateTransform3D initalRotationZ = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 100));
            initalRotationZ.CenterX = torso_centroid[0];
            initalRotationZ.CenterY = torso_centroid[1];
            initalRotationZ.CenterZ = torso_centroid[2];

           // componentTransform.Children.Add(initalRotationZ);

            RotateTransform3D initalRotationX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 20));
            initalRotationX.CenterX = torso_centroid[0];
            initalRotationX.CenterY = torso_centroid[1];
            initalRotationX.CenterZ = torso_centroid[2];

           // componentTransform.Children.Add(initalRotationX);

            skeleton.Transform = componentTransform;

            modelVisual3D = new ModelVisual3D();
            modelVisual3D.Content = skeleton;
            this.m_helix_viewport.Children.Add(modelVisual3D);
        

            Avatar.DataContext = this;
            pdr = new ProcessedDataReader(input_data);

            var x = torso.Transform.Value;
            var y = rightForearm.Transform.Value;
            int i = 0;

        }

        public void button(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("BUTTOn");
            animationTimer = new System.Windows.Threading.DispatcherTimer();
            animationTimer.Interval = new TimeSpan(50 * 1000);
            animationTimer.Tick += OnTimedEvent;
            animationTimer.Start();
         



        }

        private Quaternion wrist(Quaternion input)
        {
            return new Quaternion(input.Y,-input.X,input.Z,input.W);
        }

        private Quaternion chest(Quaternion input)
        {
            return new Quaternion(input.Y, -input.X, input.Z, input.W);
        }

        private Quaternion upperarm(Quaternion input)
        {
            return new Quaternion(input.Y, -input.X, input.Z, input.W);
        }

        private Quaternion forearm(Quaternion input)
        {
            //return input;
            Quaternion inv = new Quaternion(-input.X, input.Y, input.Z, input.W);
            Quaternion n = new Quaternion(new Vector3D(1, 0, 0), 45);
            return Quaternion.Multiply(n, inv);
            //return inv;
            
        }



        private void OnTimedEvent(object source, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                //System.Diagnostics.Debug.WriteLine(step);
                
                if (step == pdr.quaternions.Count - 1)
                {
                    animationTimer.Stop();
                    return;
                }
                int[] order = {2,0,1,3 };
            for (var x = 0; x < 4; x++) {
                Quaternion from = pdr.initial_quaternions[order[x]]; 
                Quaternion to = pdr.quaternions[step + 1][order[x]];
               // Quaternion delta = Quaternion.Subtract(to, from);
                    from.Invert();
                    Quaternion delta2 = Quaternion.Multiply(to, from);
                    Quaternion current = cm.current_quaternions[order[x]];
                    //ready[x] = cm.is_ready(order[x]);
                    if (previous[x] == Quaternion.Identity) //previous[x] == Quaternion.Identity)
                    {
                        animate(Quaternion.Identity, x);
                        previous[x] = current; // Quaternion.Multiply(offset, current); 
                    } else
                    {
                        if(count[x] == 1)
                        {
                            System.Diagnostics.Debug.WriteLine("BEGIN");
                            count[x] = 0;
                        }

                        Quaternion last = previous[x];
                        last.Invert();
                        Quaternion delta = Quaternion.Multiply(current, last);
                        //orientations[x] = Quaternion.Multiply(orientations[x], delta);
                        animate(delta, x);
                        //previous[x] = current;
                    }

                }
            step++;
            });
        }


        private void animate(Quaternion quat, int x)
        {
           
            Model3D joint = components[x];
            double[] centroid = centroids[x];
            var componentTransform = new Transform3DGroup();
            Point3D origin = new Point3D();
            
            if (x != 0) {

            Point3D original_origin = new Point3D(centroid[0], centroid[1], centroid[2]);
            var motionOffset = new Transform3DGroup();
      
            motionOffset.Children.Add(components[x-1].Transform);

            // We need to find out where our old point is now
            origin = motionOffset.Transform(original_origin);
         
            TranslateTransform3D origin_translation = new TranslateTransform3D(new Vector3D(origin.X - original_origin.X, origin.Y - original_origin.Y, origin.Z - original_origin.Z));
            componentTransform.Children.Add(origin_translation);
            }
            if (x == 3)
            {
                quat = wrist(quat) ;
                //System.Diagnostics.Debug.WriteLine(quat);
            }
            if(x == 0)
            {
                quat = chest(quat);
            }
            if(x == 1)
            {
                quat = upperarm(quat);
            }
            if(x == 2)
            {
                quat = forearm(quat);
            }
            RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(quat));

            // The point of rotation
            if (x == 0)
            {
                transform.CenterX = centroid[0];
                transform.CenterY = centroid[1];
                transform.CenterZ = centroid[2];
                
            } else
            {
                transform.CenterX = origin.X;
                transform.CenterY = origin.Y;
                transform.CenterZ = origin.Z;
            }
            componentTransform.Children.Add(transform);

            // Apply transformation
            joint.Transform = componentTransform;
        }

        public void connect(object sender, RoutedEventArgs e)
        {
            cm.connection_event();
        }

        public void stream(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.Sleep(5000);
            cm.stream_data("");
            animationTimer = new System.Windows.Threading.DispatcherTimer();
            animationTimer.Interval = new TimeSpan(20 * 10000);
            animationTimer.Tick += OnTimedEvent;
            animationTimer.Start();
        }
    }
}
