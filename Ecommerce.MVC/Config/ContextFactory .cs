using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Ecommerce.MVC.Config;

public class ContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        optionsBuilder.UseNpgsql("Host=ep-patient-tooth-aepjcu1a-pooler.c-2.us-east-2.aws.neon.tech; Database=neondb; Username=neondb_owner; Password=npg_FzTjIn9pRlr2; SSL Mode=VerifyFull; Channel Binding=Require;");

        return new DatabaseContext(optionsBuilder.Options);
    }
}