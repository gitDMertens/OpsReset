using GenericParsing;
using System;
using System.Data;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Text;
using CarLocation.Helpers;

namespace CarLocation.JMRI
{
    public sealed class CarRoster
    {
        //This variable is going to store the Singleton Instance
        private static CarRoster Instance = null;

        //The following Static Method is going to return the Singleton Instance
        public static CarRoster GetInstance()
        {
            //If the variable instance is null, then create the Singleton instance 
            //else return the already created singleton instance
            //This version is not thread safe
            if (Instance == null)
            {
                Instance = new CarRoster();
            }

            //Return the Singleton Instance
            return Instance;
        }

        private DataTable roster;

        //Constructor is Private means, from outside the class we cannot create an instance of this class
        private CarRoster()
        {
            using (GenericParserAdapter parser = new GenericParserAdapter(ConfigurationManager.AppSettings["CarRosterFileName"]))
            {
                parser.ColumnDelimiter = ',';
                parser.FirstRowHasHeader = true;
                //parser.SkipStartingDataRows = 10;
                parser.MaxBufferSize = 4096;
                parser.MaxRows = 500;
                parser.TextQualifier = '\"';
                roster = parser.GetDataTable();
                
                parser.Close();
            }
        }

        //The following can be accesed from outside of the class by using the Singleton Instance
        public List<Car> GetCars(Car carCriteria)
        {
            List<Car> results = new List<Car>();

            var collection = roster.AsEnumerable();
            if (carCriteria.Location != null && carCriteria.Location != string.Empty) collection = collection.Where(c => c.Field<string>("Location") == carCriteria.Location);
            if (carCriteria.Track != null && carCriteria.Track != string.Empty)       collection = collection.Where(c => c.Field<string>("Track") == carCriteria.Track);
            if (carCriteria.Road != null && carCriteria.Road != string.Empty)         collection = collection.Where(c => c.Field<string>("Road") == carCriteria.Road);
            if (carCriteria.Number != null && carCriteria.Number != string.Empty)     collection = collection.Where(c => c.Field<string>("Number") == carCriteria.Number);
            if (carCriteria.Type != null && carCriteria.Type != string.Empty)         collection = collection.Where(c => c.Field<string>("Type") == carCriteria.Type);
            if (carCriteria.Length != null && carCriteria.Length != string.Empty)     collection = collection.Where(c => c.Field<string>("Length") == carCriteria.Length);
            if (carCriteria.Color != null && carCriteria.Color != string.Empty)       collection = collection.Where(c => c.Field<string>("Color") == carCriteria.Color);

            if (collection.Any())
            {
                results = collection.ToList()
                    .Select(r => new Car()
                    {
                        Road = r.Field<string>("Road"),
                        Number = r.Field<string>("Number"),
                        Type = r.Field<string>("Type"),
                        Length = r.Field<string>("Length"),
                        Color = r.Field<string>("Color"),
                    }).ToList();
            }
            return results;
        }

        public QueryResponse<Car> SetLocation(Car carCriteria)
        {
            QueryResponse<Car> response = new QueryResponse<Car>();
            LocationRoster locationRoster = LocationRoster.GetInstance();
            TrackDetail trackDetail = locationRoster.GetTrackDetails(carCriteria.Location, carCriteria.Track);

            // Get the car based on the number (and Road if provided)
            Car targetCriteria = new Car() { Number = carCriteria.Number };
            if (!string.IsNullOrEmpty(carCriteria.Road))
            {
                targetCriteria.Road = carCriteria.Road;
            }
            Car target = GetCars(targetCriteria).First();

            if (!trackDetail.ValidCarTypes.Any(vc => vc.Contains(target.Type)))
            {
                response.result = "false";
                response.message = string.Format("{0} is of type {1} which is not allowed on this track.", target.Number, target.Type);
            }
            else
            {
                // Check track length versus length of cars on the track
                Car criteria = new Car() { Location = carCriteria.Location, Track = carCriteria.Track };
                List<Car> carsOnTrack = GetCars(criteria);
                int usedLength = carsOnTrack.Sum(u => int.Parse(u.Length));
                if (usedLength + int.Parse(target.Length) > trackDetail.TrackLength)
                {
                    response.result = "false";
                    response.message = string.Format("The length of {0} exceeds the available space on this track.", target.Number);
                }
                else
                {
                    var collection = roster.AsEnumerable();
                    collection = collection.Where(c => c.Field<string>("Number") == target.Number);
                    collection = collection.Where(c => c.Field<string>("Road") == target.Road);
                    DataRow dr = collection.FirstOrDefault();
                    if (dr != null)
                    {
                        dr["Location"] = carCriteria.Location;
                        dr["Track"] = carCriteria.Track;

                        response.result = "success";
                        response.data = GetCars(carCriteria);
                    }
                    else
                    {
                        response.result = "failure";
                        response.message = "Car not found";
                    }
                }
            }
            return response;
        }

        public QueryResponse<Car> RemoveLocation(Car carCriteria)
        {
            QueryResponse<Car> results = new QueryResponse<Car>();

            var collection = roster.AsEnumerable();
            if (carCriteria.Road != null && carCriteria.Road != string.Empty) collection = collection.Where(c => c.Field<string>("Road") == carCriteria.Road);
            if (carCriteria.Number != null && carCriteria.Number != string.Empty) collection = collection.Where(c => c.Field<string>("Number") == carCriteria.Number);
            if (carCriteria.Location != null && carCriteria.Location != string.Empty) collection = collection.Where(c => c.Field<string>("Location") == carCriteria.Location);
            if (carCriteria.Track != null && carCriteria.Track != string.Empty) collection = collection.Where(c => c.Field<string>("Track") == carCriteria.Track);
            DataRow dr = collection.FirstOrDefault();
            if (dr != null)
            {
                dr["Location"] = ConfigurationManager.AppSettings["MissingCarLocation"];
                dr["Track"] = ConfigurationManager.AppSettings["MissingCarTrack"];

                results.result = "success";
                results.data = GetCars(carCriteria);
            }
            else
            {
                results.result = "failure";
                results.message = "Car not found on that track.  This could mean the car was already relocated.";
            }

            return results;
        }

        public void Save()
        {
            // https://stackoverflow.com/questions/4959722/how-can-i-turn-a-datatable-to-a-csv
            StringBuilder fileContent = new System.Text.StringBuilder();

            foreach (var col in roster.Columns)
            {
                if (col == null)
                    fileContent.Append(",");
                else
                    fileContent.Append("\"" + col.ToString().Replace("\"", "\"\"") + "\",");
            }
            fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);

            foreach (DataRow dr in roster.Rows)
            {
                foreach (var column in dr.ItemArray)
                {
                    if (column == null)
                        fileContent.Append(",");
                    else
                        fileContent.Append("\"" + column.ToString().Replace("\"", "\"\"") + "\",");
                }
                fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
            }

            System.IO.File.WriteAllText(ConfigurationManager.AppSettings["CarRosterFileName"], fileContent.ToString());
        }
    }
}
