using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CarLocation.Helpers
{
    public class QueryResponse<T>
    {
        public string result;
        public string message;

        public List<T> data;

        public QueryResponse()
        {
            data = new List<T>();
        }
    }
}