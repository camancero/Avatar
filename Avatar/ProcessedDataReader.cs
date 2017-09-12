using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Avatar
{

    class ProcessedDataReader
    {
        public Quaternion[] initial_quaternions = new Quaternion[4];
        public Quaternion[] current_quaternions = new Quaternion[4];
        public List<Quaternion[]> quaternions = new List<Quaternion[]>();

        private int[] quarternion_columns = {0,1,2,3,5,6,7,8,10,11,12,13,15,16,17,18};
        public ProcessedDataReader(string file_path)
        {
            Task get_data = Task.Run(() =>  process_csv(file_path));
            while (!get_data.IsCompleted)
            {
            }
            int i = 0;
        }

        private void process_csv(string file_path)
        {
            using (var fs = File.OpenRead(file_path))
            using (var reader = new StreamReader(fs))
            {
                int line_index = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    string[] data = line.Split(',');
                    if(line_index == 0)
                    {
                        set_quaternions(data, initial_quaternions);
                        quaternions.Add(initial_quaternions);

                    } else
                    {
                        set_quaternions(data, current_quaternions);
                        quaternions.Add((Quaternion[])current_quaternions.Clone());
                    }
                    line_index++;


                }
            }
 
        }

        private void set_quaternions(string[] data, Quaternion[] list)
        {
            var comp = 0;
            var q_count = 0;
            double[] components = new double[4];
            for (var i=0; i < quarternion_columns.Length; i++)
            {
                
                components[comp] = Convert.ToDouble(data[quarternion_columns[i]]);
                comp++;
                if (comp > 3)
                {
                    list[q_count] = new Quaternion(components[1], components[2], components[3], components[0]);
                    comp = 0;
                    q_count++;
                    components = new double[4];
                }
            }
        }
    }
}
