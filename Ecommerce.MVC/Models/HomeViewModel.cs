using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Models;

public class HomeViewModel
{
     public IEnumerable<Categoria> Categorias { get; set; } = Enumerable.Empty<Categoria>();
}