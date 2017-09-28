using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.IO;
using System;
using System.Collections.Generic;
using NUClass;
using hello;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using System.Linq;

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
        private Quaternion ninetyx = new Quaternion(0, 0.7071, 0.7071, 0);
        ConnectionManager cm = new ConnectionManager();


        private List<double[]> centroids = new List<double[]>();
        private Quaternion[] initial_frame = new Quaternion[4];

         

       
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
        System.Windows.Threading.DispatcherTimer animationTimer;
        private int step = 1;
        public Model3D AvatarModel { get; set; }
        public Model3D[] components;
        public Quaternion[] orientations = new Quaternion[4];
        public Quaternion[] reference = new Quaternion[4];
        int[] count = { 2, 2, 2, 2 };
        int[] ready = { 0, 0, 0, 0 };


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_helix_viewport.ModelUpDirection = new Vector3D(0, 1, 0);

            //helloClass X = new helloClass();
            
            //MWArray[] result = X.hello(1);
            //System.Diagnostics.Debug.WriteLine((MWNumericArray)result[0]);
            populate_reference_data();
            initial_frame[0] = new Quaternion(-0.330925973595304, -0.29698484809835, 0.524673231640418, 0.725491557497398);
            initial_frame[1] = new Quaternion(-0.323147799002252, 0.173241161390704, 0.143542676580869, -0.918531708761325);
            initial_frame[2] = new Quaternion(0.580534667354156, -0.00565685424949242, 0.410121933088198, -0.703571247280615);
            initial_frame[3] = new Quaternion(0.399515331370399, -0.20081832585698, 0.526087445202791, -0.723370237153838);

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
            //skeleton.Children.Add(rightHand);

            components = new Model3D[] { torso, rightUpperArm, rightForearm, rightHand };

            var componentTransform = new Transform3DGroup();

            RotateTransform3D initalRotationZ = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 100));
            initalRotationZ.CenterX = centroids[0][0];
            initalRotationZ.CenterY = centroids[0][1];
            initalRotationZ.CenterZ = centroids[0][2];

            //componentTransform.Children.Add(initalRotationZ);

            RotateTransform3D initalRotationX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 20));
            initalRotationX.CenterX = centroids[0][0];
            initalRotationX.CenterY = centroids[0][1];
            initalRotationX.CenterZ = centroids[0][2];

            //componentTransform.Children.Add(initalRotationX);

            skeleton.Transform = componentTransform;

            modelVisual3D = new ModelVisual3D();
            modelVisual3D.Content = skeleton;
            this.m_helix_viewport.Children.Add(modelVisual3D);


            Avatar.DataContext = this;
            //pdr = new ProcessedDataReader(input_data);

        }

        private void populate_reference_data()
        {
            //TODO:  LEFT FOREARM Z= 54.4
            centroids.Add(new double[]{ 0.83, 4.63, 56.9 }); // Torso
            centroids.Add(new double[] { 27.8, 4.89, 100 }); // Right Upper Arm
            centroids.Add(new double[] { 27.8, 4.89, 54.4 }); // Right Forearm
            centroids.Add(new double[] { 27.8, 4.89, 8.32 }); // Right Hand
            centroids.Add(new double[] { -29.1, 4.89, 100 }); // Left Upper Arm
            centroids.Add(new double[] { -29.1, 4.89, 54.4 }); // Left Forearm
            centroids.Add(new double[] { -29.1, 4.89, 8.32 }); // Left Hand


    }

    public void button(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("BUTTOn");
            animationTimer = new System.Windows.Threading.DispatcherTimer();
            animationTimer.Interval = new TimeSpan(50 * 1000);
            animationTimer.Tick += OnTimedEvent;
            animationTimer.Start();
        }


        private Quaternion chest(Quaternion q)
        {
            Quaternion rotation_a = new Quaternion(new Vector3D(0, 0, 1), -90);
            Quaternion rotation_a_conj = rotation_a;
            rotation_a_conj.Conjugate();
            Quaternion rot = Quaternion.Multiply(Quaternion.Multiply(rotation_a, q), rotation_a_conj);
            return rot;
        }

        private Quaternion UA(Quaternion q)
        {
            Quaternion offset = Quaternion.Multiply(q, offsets[1]);
            return offset;
            
            /*
            Quaternion rotation_a = new Quaternion(new Vector3D(0, 1, 0), 45);
            Quaternion rotation_a_conj = rotation_a;
            rotation_a_conj.Conjugate();
            Quaternion rot = Quaternion.Multiply(Quaternion.Multiply(rotation_a, q), rotation_a_conj);
           
            
            Quaternion x = new Quaternion(q.Y, -q.X, q.Z, q.W);

            Quaternion rotation_b = new Quaternion(new Vector3D(0, 0, 1), -45);
            Quaternion rotation_b_conj = rotation_b;
            rotation_a_conj.Conjugate();
            Quaternion man = Quaternion.Multiply(Quaternion.Multiply(rotation_b, x), rotation_b_conj);
            System.Diagnostics.Debug.WriteLine(q + " // " + rot + " // " + man);
            return man; 
            */

        }

        private Quaternion FA(Quaternion q)
        {
            Quaternion x = new Quaternion(q.Y, -q.X, q.Z, q.W);

            Quaternion rotation_a = new Quaternion(new Vector3D(0, 0, 1), -45);
            Quaternion rotation_a_conj = rotation_a;
            rotation_a_conj.Conjugate();
            return Quaternion.Multiply(Quaternion.Multiply(rotation_a, x), rotation_a_conj);
        }

        private Quaternion WR(Quaternion q)
        {
            Quaternion rot = Quaternion.Identity;
            if (q != Quaternion.Identity)
            {
                Quaternion rotation_a = new Quaternion(new Vector3D(0, 0, 1), -90);
                Quaternion rotation_a_conj = rotation_a;
                rotation_a_conj.Conjugate();
                rot = Quaternion.Multiply(Quaternion.Multiply(rotation_a, q), rotation_a_conj);
            }
            return rot;
        }

        private Quaternion[] offsets = new Quaternion[4];

        private void OnTimedEvent(object source, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int[] order = {2,0,1,3 };
            for (var x = 0; x < 4; x++) {
                    Quaternion current = cm.current_quaternions[order[x]];
                    if(current != Quaternion.Identity)
                    {
                        //current.Normalize();
                        if (x == 0)
                        {
                            current = chest(current);
                        }
                        
                        if (count[x] == 2) //If first read
                        {
                            reference[x] = current;
                            count[x] = 1;
                            System.Diagnostics.Debug.WriteLine(x + ": Reference Set.");
                        } else if (count.Sum() <= 4)
                        {

                            if(count[x] == 1)
                            {
                                Quaternion chest_ref = reference[0];
                                chest_ref.Invert();
                                offsets[x] =  Quaternion.Multiply(reference[0], chest_ref); 
                                if(x == 0)
                                {
                                    //offsets[x] = Quaternion.Identity;
                                }
                                count[x] = 0;
                                System.Diagnostics.Debug.WriteLine(x + ": Offset Calculated. " + (offsets[x] ==Quaternion.Identity) + " - " +  offsets[x] + " - " + reference[x]);
                            } else if (count[x] == 0)
                            {

                        Quaternion ref_frame = reference[x];
                        ref_frame.Invert();
                        
                                Quaternion offset = Quaternion.Multiply(current, offsets[x]);
                                Quaternion delta = Quaternion.Multiply(offset, ref_frame);
                                animate(delta, x);
               
                            }


                        }

                    }
                    


         
                }
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

            switch (x)
            {
                case 0:
                    //quat = chest(quat);
                    break;
                case 1:
                   // quat = UA(quat);
                    break;
                case 2:
                  //  quat = FA(quat);
                    break;
                case 3:
                   // quat = WR(quat);
                    break;
                default:
                    Console.WriteLine("Wut?");
                    break;
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
