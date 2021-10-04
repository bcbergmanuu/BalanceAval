using System;
using System.Collections.Generic;
using BalanceAval.Models;

namespace BalanceAval.Service
{
    public interface IReadNidaq
    {

        public event EventHandler<List<AnalogChannel>> DataReceived;
        void Start();
        void Stop();
    }
}