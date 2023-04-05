using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CarLocation.JMRI;
using CarLocation.Helpers;

namespace CarLocation.Controllers
{
    public class HomeController : Controller
    {
        LocationRoster locationRoster = LocationRoster.GetInstance();
        CarRoster carRoster = CarRoster.GetInstance();

        public ActionResult Index()
        {
            List<string> locations = locationRoster.GetLocations();
            List<string> tracks = locationRoster.GetTracks(locations.First());
            return View();
        }

        public ActionResult getLocations()
        {
            return Json(locationRoster.GetLocations().Select(x => new
            {
                displayText = x,
                value = x
            }).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult getTracks(string location)
        {
            return Json(locationRoster.GetTracks(location).Select(x => new
            {
                displayText = x,
                value = x
            }).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult getCars(string track, string location)
        {
            Car criteria = new Car() { Track = track, Location = location };
            List<Car> cars = carRoster.GetCars(criteria);
            DataTableResponse<DataTableRow> response = new DataTableResponse<DataTableRow>();
            foreach(Car car in cars)
            {
                response.data.Add(new DataTableRow
                {
                    DT_RowData = car,
                    DT_RowId = response.data.Count().ToString(),
                    Color = car.Color,
                    Length = car.Length,
                    Location = car.Location,
                    Number = car.Number,
                    Road = car.Road,
                    Track = car.Track,
                    Type = car.Type
                });
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult setCarLocation(string track, string location, string number)
        {
            QueryResponse<Car> response = new QueryResponse<Car>();
            Car criteria = new Car() { Number = number };
            List<Car> carDetail = carRoster.GetCars(criteria);
            if (carDetail.Count == 0)
            {
                response.result = "false";
                response.message = string.Format("Car number {0} is not defined.", number);
            }
            else
            {
                if (carDetail.Count > 1)
                {
                    response.result = "false";
                    response.message = string.Format("There are multiple cars with the number {0}.  This program can't set the location.", number);
                }
                else
                {
                    criteria.Location = location;
                    criteria.Track = track;
                    response = carRoster.SetLocation(criteria);
                }
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RemoveCarLocation(string track, string location, string number, string road)
        {
            Car criteria = new Car() { Number = number, Location = location, Track = track, Road = road };
            QueryResponse<Car> response = carRoster.RemoveLocation(criteria);
            return Json(response, JsonRequestBehavior.AllowGet);
        }
    }
}