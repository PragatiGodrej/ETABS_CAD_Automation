using ETABSv1;

namespace ETABS_CAD_Automation.Core
{
    /// <summary>
    /// Manages material definitions in ETABS
    /// </summary>
    public class MaterialManager
    {
        private readonly cSapModel sapModel;

        public MaterialManager(cSapModel model)
        {
            sapModel = model;
        }

        /// <summary>
        /// Define standard materials (Concrete, Steel, etc.)
        /// </summary>
        public void DefineMaterials()
        {
            DefineConcrete();
            DefineSteel();
        }

        /// <summary>
        /// Define standard concrete material (M25)
        /// </summary>
        private void DefineConcrete()
        {
            const string matName = "CONC";
            const double E = 25000000; // kN/m² (25 GPa)
            const double poisson = 0.2;
            const double thermalCoeff = 0.0000099; // per °C

            sapModel.PropMaterial.SetMaterial(matName, eMatType.Concrete);
            sapModel.PropMaterial.SetMPIsotropic(matName, E, poisson, thermalCoeff);
        }

        /// <summary>
        /// Define standard steel material (optional - for future use)
        /// </summary>
        private void DefineSteel()
        {
            const string matName = "STEEL";
            const double E = 200000000; // kN/m² (200 GPa)
            const double poisson = 0.3;
            const double thermalCoeff = 0.0000117; // per °C

            sapModel.PropMaterial.SetMaterial(matName, eMatType.Steel);
            sapModel.PropMaterial.SetMPIsotropic(matName, E, poisson, thermalCoeff);
        }

        /// <summary>
        /// Define custom concrete grade
        /// </summary>
        public void DefineCustomConcrete(string name, double fck)
        {
            // E = 5000 * sqrt(fck) in MPa, convert to kN/m²
            double E = 5000 * System.Math.Sqrt(fck) * 1000;
            const double poisson = 0.2;
            const double thermalCoeff = 0.0000099;

            sapModel.PropMaterial.SetMaterial(name, eMatType.Concrete);
            sapModel.PropMaterial.SetMPIsotropic(name, E, poisson, thermalCoeff);
        }
    }
}