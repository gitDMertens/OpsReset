using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CarLocation.Helpers
{
    public class DataTableResponse<T>
    {
        public int draw;
        public int recordsTotal { get { return data.Count; } }
        public int recordsFiltered { get { return data.Count; } }

        public List<T> data;

        public DataTableResponse()
        {
            draw = 1;
            data = new List<T>();
        }
    }
}