using System.Collections.Generic;
using SCParse;

namespace SCGenerator
{
   

    public abstract class SCBasePenPathGenerator
    {
        protected float viewBoxToPaperScale = 1f;
        protected GeneratorConfig generatorConfig;
        protected MachineConfig machineConfig;

        public SCBasePenPathGenerator(
            MachineConfig machineConfig,
            Box2 viewBox,
            GeneratorConfig generatorConfig
            )
        {
            this.generatorConfig = generatorConfig;
            this.machineConfig = machineConfig;
            viewBoxToPaperScale = viewBox.getFitScale(machineConfig.paper.size);
        }

        //
        // Yield return some drawing paths.
        // don't add pull up/ push down moves between drawing paths.
        // Penetration depth is binary (only either up or down since this is
        // only meant to model a pen at the moment).
        //
        public abstract IEnumerable<PenDrawingPath> generate();

        public virtual string GetProcessIntensityComments()
        {
            return "No process time estimates";
        }
    }
}
