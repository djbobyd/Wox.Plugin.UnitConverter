using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnitsNet;

namespace Wox.Plugin.Units
{
    public class UnitsPlugin : IPlugin
    {
        #region private fields
        private PluginInitContext _context;
        private readonly string checkPattern = @"(\d+(\.\d{1,2})?)?\s([A-Za-z]*)\s([i][n])\s([A-Za-z]*)"; // 10 meter in centimeter
        private string _toUnit = "";
        private string _fromUnit = "";
        private double _quantity = new double();
        private double _result = new double();
        private static readonly Assembly UnitsNetAssembly = Assembly.GetAssembly(typeof(Length));
        #endregion

        public UnitsPlugin()
        {

        }
        public void Init(PluginInitContext context)
        {
            _context = context;
        }
        public List<Result> Query(Query query) {
            List<Result> results = new List<Result>();
            try
            {
                if (Regex.IsMatch(query.Search, checkPattern))
                {
                    if (query.RawQuery != null && query.RawQuery.Split(' ').Length == 4)
                    {
                        _quantity = Convert.ToDouble(query.FirstSearch);
                        _fromUnit = query.SecondSearch;
                        _toUnit = query.RawQuery.Split(' ')[3];
                        _result = Calculate(_fromUnit, _toUnit, _quantity);
                    }
                    results.Add(new Result
                    {
                        Title = "Converting: " + _quantity + " " + _fromUnit + " to " + _result + " " + _toUnit,
                        SubTitle = "",
                        IcoPath = "Images/plugin.png",  //relative path to your plugin directory
                        Action = e =>
                        {
                        // after user select the item

                        // return false to tell Wox don't hide query window, otherwise Wox will hide it automatically
                        return false;
                        }
                    });
                }
            return results;
            }
            catch (Exception)
            {
                return new List<Result>();
            }
        }

        private double Calculate(string fromUnit, string toUnit, double quantity)
        {
            double result = 0.0;
            bool isMatch = false;
            Type[] tp = GetTypesInNamespace(UnitsNetAssembly, "UnitsNet");

            foreach (Type t in tp)
            {
                isMatch = UnitConverter.TryConvertByAbbreviation(quantity, t.Name.ToString(), fromUnit, toUnit, out result);
                if (isMatch)
                {
                    break;
                }
                isMatch = UnitConverter.TryConvertByName(quantity, t.Name.ToString(), fromUnit, toUnit, out result);
                if (isMatch)
                {
                    break;
                }
            }
            return result;
        }
        private static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }
    }
}
