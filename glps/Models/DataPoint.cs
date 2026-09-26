using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace glps.Models
{
	[DataContract]
	public class DataPoint
    {
		public DataPoint(string label, double y)
		{
			this.Label = label;
			this.Y = y;
		}

		public DataPoint(string label, double y, int count) : this(label, y)
		{
			this.Count = count;
		}

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "label")]
		public string Label = "";

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "y")]
		public Nullable<double> Y = null;

		//Number of passengers behind the value (shown in chart tooltips).
		[DataMember(Name = "count", EmitDefaultValue = false)]
		public Nullable<int> Count = null;

	}
}
