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
        optionsBuilder.UseNpgsql("Host=ep-restless-bar-ahnlpuzt-pooler.c-3.us-east-1.aws.neon.tech; Database=ecommerce; Username=neondb_owner; Password=npg_4duekG1SxqcV; SSL Mode=VerifyFull; Channel Binding=Require;");

        return new DatabaseContext(optionsBuilder.Options);
    }
}