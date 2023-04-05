using GenericParsing;
using System;
using System.Data;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace CarLocation.JMRI
{
    public sealed class LocationRoster
    {
        //This variable is going to store the Singleton Instance
        private static LocationRoster Instance = null;

        //The following Static Method is going to return the Singleton Instance
        public static LocationRoster GetInstance()
        {
            //If the variable instance is null, then create the Singleton instance 
            //else return the already created singleton instance
            //This version is not thread safe
            if (Instance == null)
            {
                Instance = new LocationRoster();
            }

            //Return the Singleton Instance
            return Instance;
        }

        private DataTable roster;

        //Constructor is Private means, from outside the class we cannot create an instance of this class
        private LocationRoster()
        {
            using (GenericParserAdapter parser = new GenericParserAdapter(ConfigurationManager.AppSettings["LocationRosterFileName"]))
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

        public List<string> GetLocations()
        {
            List<string> results = roster.AsEnumerable()
                                    .Select(r => r.Field<string>("Location"))
                                    .Distinct().ToList();

            return results;
        }

        public List<string> GetTracks(string location)
        {
            List<string> results = new List<string>();
            var collection = roster.AsEnumerable()
                                    .Where(r =>r.Field<string>("Location") == location)
                                    .Select(r => r.Field<string>("Track"))
                                    .Distinct();
            if (collection.Any())
            {
                results = collection.ToList();
            }
            return results;
        }

        public TrackDetail GetTrackDetails(string location, string track)
        {
            TrackDetail results = roster.AsEnumerable()
                                    .Where(r => r.Field<string>("Location") == location)
                                    .Where(r => r.Field<string>("Track") == track)
                                    .Select(r => new TrackDetail()
                                    {
                                        TrackLength = int.Parse(r.Field<string>("Length")),
                                        ValidCarTypes = r.Field<string>("Rolling Stock").Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList()
                                    })
                                    .FirstOrDefault();

            return results;
        }
    }
}
