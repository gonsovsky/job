namespace Atoll.Transport.ServerBundle
{
    public class AssignedUnitIdProvider : IUnitIdProvider
    {
        //private Timer timer;
        private string unitId;

        public AssignedUnitIdProvider(string unitId)
        {
            this.unitId = unitId;
        }

        public string GetId()
        {
            return this.unitId;
        }
    }
}
