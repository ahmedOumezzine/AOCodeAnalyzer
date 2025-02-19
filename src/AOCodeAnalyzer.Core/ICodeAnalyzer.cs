using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.Core
{
    public interface ICodeAnalyzer<T> where T : IAnalysisResult
    {
        /// <summary>
        /// Exécute l'analyse sur le code fourni
        /// </summary>
        void Analyze(string code);

        /// <summary>
        /// Affiche le rapport d'analyse dans la console
        /// </summary>
        void DisplayReport();

        /// <summary>
        /// Retourne les résultats bruts de l'analyse
        /// </summary>
        IEnumerable<T> GetResults();
    }
}
