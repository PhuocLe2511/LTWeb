using SV22T1020330.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020330.Models.Cart
{
    public class CartItem
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
    }
}

