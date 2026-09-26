using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using glps.Models;

namespace glps.DAL
{
    public class glpsContext : DbContext
    {
        public glpsContext() : base("name=glpsConnectionString")
        {
        }

        public DbSet<airline_details> airline_Details { get; set; }
        public DbSet<airport_data>  airport_Datas{ get; set; }
        public DbSet<appin_data> appin_Datas { get; set; }

        public DbSet<asset_details> asset_Details { get; set; }
        public DbSet<bchn_data> bchn_Datas { get; set; }

        public DbSet<User> users { get; set; }

    }
}