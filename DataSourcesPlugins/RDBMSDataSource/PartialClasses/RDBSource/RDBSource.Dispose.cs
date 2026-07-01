using System;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "dispose"
        private bool _rdsDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_rdsDisposed)
            {
                if (disposing)
                {
                    Closeconnection();
                    Entities = null;
                    EntitiesNames = null;
                }

                _rdsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        #endregion
    }
}
