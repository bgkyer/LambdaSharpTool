using System;
using System.Threading.Tasks;
using LambdaSharp;
using LambdaSharp.Finalizer;

namespace Legacy.ModuleV080.Finalizer {

    public sealed class Function : ALambdaFinalizerFunction {

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public override async Task CreateDeployment(FinalizerProperties current) {

            // TO-DO: add business logic when creating a CloudFormation stack
        }

        public override async Task UpdateDeployment(FinalizerProperties next, FinalizerProperties previous) {

            // TO-DO: add business logic when updating a CloudFormation stack
        }

        public override async Task DeleteDeployment(FinalizerProperties current) {

            // TO-DO: add business logic when deleting a CloudFormation stack
        }
    }
}
