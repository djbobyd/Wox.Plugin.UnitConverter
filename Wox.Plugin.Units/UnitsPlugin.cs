using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnitsNet;
using UnitsNet.Units;

namespace Wox.Plugin.Units
{
    public class UnitsPlugin : IPlugin
    {
        #region private fields
        private PluginInitContext _context;
        private readonly string checkPattern = @"(\d+(\.\d{1,2})?)?\s([A-Za-z]*)\s([i][n])\s([A-Za-z]*)"; // 10 meter in centimeter
        private readonly string unitPattern = @"([A-Za-z]*)\s([u][n][i][t][s])"; // Length units
        private readonly string valuePattern = @"([L][i][s][t])\s([v][a][l][u][e][s])"; // List values
        private string _toUnit = "";
        private string _fromUnit = "";
        private double _quantity;
        private double _result;
        private Type[] _valueTypes;
        private Type[] _unitTypes;
        private static readonly Assembly UnitsNetAssembly = Assembly.GetAssembly(typeof(Length));
        private static readonly string QuantityNamespace = typeof(Length).Namespace;
        private static readonly string UnitTypeNamespace = typeof(LengthUnit).Namespace;
        #endregion

        public UnitsPlugin()
        {
        }
        public void Init(PluginInitContext context)
        {
            _context = context;
            _valueTypes=GetQuantityTypes(GetTypesInNamespace(UnitsNetAssembly,QuantityNamespace));
            _unitTypes=GetUnitTypes(GetTypesInNamespace(UnitsNetAssembly,UnitTypeNamespace));
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
                        IcoPath = "Images/convert.png",  //relative path to your plugin directory
                        Action = e => false
                    });
                } else if (Regex.IsMatch(query.Search,unitPattern))
                {
                    Array units=listUnits(query.FirstSearch);
                    if (units == null) return results;
                    foreach (object unit in units)
                    {
                        if (unit.ToString().Equals("Undefined")) continue;
                        results.Add(new Result
                        {
                            Title = unit.ToString(),
                            SubTitle = "",
                            IcoPath = "Images/convert.png",
                            Action = e => false
                        });
                    }
                } else if (Regex.IsMatch(query.Search, valuePattern))
                {
                    foreach (Type t in _valueTypes)
                    {
                        results.Add(new Result
                        {
                            Title = t.Name,
                            SubTitle = "",
                            IcoPath = "Images/convert.png",  //relative path to your plugin directory
                            Action = e => false
                        });
                    }
                }
            return results;
            }
            catch (Exception)
            {
                return new List<Result>();
            }
        }

        private Array listUnits(string queryFirstSearch)
        {
            Array units = null;
            foreach (Type t in _unitTypes)
            {
                if (!t.Name.Equals(queryFirstSearch + "Unit")) continue;
                units = t.GetEnumValues();
            }

            return units;
        }

        private double Calculate(string fromUnit, string toUnit, double quantity)
        {
            var result = 0.0;
            var isMatch = false;

            foreach (Type t in _valueTypes)
            {
                isMatch = UnitConverter.TryConvertByAbbreviation(quantity, t.Name, fromUnit, toUnit, out result);
                if (isMatch)
                {
                    break;
                }
                isMatch = UnitConverter.TryConvertByName(quantity, t.Name, fromUnit, toUnit, out result);
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
        private static Type[] GetQuantityTypes(Type[] tp)
        {
            return tp.Where(t => t.BaseType.Name.Equals("ValueType") && !t.Name.Equals("Vector2") &&
                                 !t.Name.Equals("Vector3") &&
                                 !t.Name.Equals("QuantityValue") && !t.Name.Equals("Length2d")).ToArray();
        }

        private static Type[] GetUnitTypes(Type [] utp)
        {
            return utp.Where(t => t.IsEnum).ToArray();
        }
    }
}
