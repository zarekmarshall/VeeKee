namespace VeeKee.Shared.Ssh
{
    public abstract class SshService
    {
        public RouterConnectionResult Connection { get; set; }

        public SshService()
        {
            this.Connection = new RouterConnectionResult();
        }
    }
}