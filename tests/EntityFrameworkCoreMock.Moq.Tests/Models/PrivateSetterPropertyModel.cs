namespace EntityFrameworkCoreMock.Tests.Models
{
    public class PrivateSetterPropertyModel
    {
        private PrivateSetterPropertyModel() { }

        public PrivateSetterPropertyModel(string value)
        {
            Private = value;
        }

        public string Private { get; private set; }
    }
}