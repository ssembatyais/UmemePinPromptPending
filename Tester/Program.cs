using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UssdPaymentSender.Logic;

namespace Tester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                ThreadingLogic TL = new ThreadingLogic();
                TL.ProcessRonSourcingTransactions();
            }
        }
    }
}
