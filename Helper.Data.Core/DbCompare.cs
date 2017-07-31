using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Helper.Data
{
    public class DbCompare
    {
        private DbModel source;
        private DbModel destination;
        private Dictionary<string, string> DictMatchFields { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Func<object, object>> DictMapFields = new Dictionary<string, Func<object, object>>();

        public DbCompare(DbModel sourceModel, DbModel destinationModel, Dictionary<string, string> dictMatchFields = null)
        {
            this.source = sourceModel;
            this.destination = destinationModel;
            this.DictMatchFields = dictMatchFields;
        }

        public string MapField(string fieldName)
        {
            if (DictMatchFields.TryGetValue(fieldName, out string val))
            {
                return val;
            }
            else
            {
                return fieldName;
            }
        }

        public DbCompare MapValue(string key, Func<object, object> value)
        {
            DictMapFields.Add(key, value);

            return this;
        }

        public Tuple<DbModel, DbModel> CompareDbModels()
        {
            var diffSource = new DbModel();
            var diffDestination = new DbModel();

            Tuple<DbModel, DbModel> compareResult = new Tuple<DbModel, DbModel>(diffSource, diffDestination);

            //left to right compare
            var sourceKeys = source.Data.Keys.Except(destination.Data.Keys);    //source has desti no
            foreach (var sourceKey in sourceKeys)
            {
                var sourceData = source[sourceKey];

                if (DictMapFields.TryGetValue(sourceKey, out Func<object, object> Map))
                {
                    diffSource[MapField(sourceKey)] = Map(sourceData);
                    diffDestination[MapField(sourceKey)] = null;    //destinationData = null
                }
                else
                {
                    diffSource[MapField(sourceKey)] = sourceData;
                    diffDestination[MapField(sourceKey)] = null;
                }
            }

            //right to left compare
            var xDestinationKeys = destination.Data.Keys.Except(source.Data.Keys, StringComparer.OrdinalIgnoreCase);  //desti has source no
            foreach (var xDestinationKey in xDestinationKeys)
            {
                var destinationData = destination[xDestinationKey];

                if (DictMapFields.TryGetValue(xDestinationKey, out Func<object, object> Map))
                {
                    diffDestination[MapField(xDestinationKey)] = Map(destinationData);
                    diffSource[MapField(xDestinationKey)] = null;    //destinationData = null
                }
                else
                {
                    diffDestination[MapField(xDestinationKey)] = destinationData;
                    diffSource[MapField(xDestinationKey)] = null;
                }
            }

            //cross compare for same fields
            var intersectKeys = source.Data.Keys.Intersect(destination.Data.Keys);
            foreach (var intersectKey in intersectKeys)
            {
                var sourceData = source[intersectKey];
                var destinationData = destination[intersectKey];
                if (sourceData is IComparable)
                {
                    if (((IComparable)sourceData).CompareTo(destinationData) != 0)    //different value, need to record
                    {
                        if (DictMapFields.TryGetValue(intersectKey, out Func<object, object> Map))
                        {
                            diffSource[MapField(intersectKey)] = Map(sourceData);
                            diffDestination[MapField(intersectKey)] = Map(destinationData);
                        }
                        else
                        {
                            diffSource[MapField(intersectKey)] = sourceData;
                            diffDestination[MapField(intersectKey)] = destinationData;
                        }
                    }
                }
                else if (!sourceData.Equals(destinationData)) //different value, need to record
                {
                    if (DictMapFields.TryGetValue(intersectKey, out Func<object, object> Map))
                    {
                        diffSource[MapField(intersectKey)] = Map(sourceData);
                        diffDestination[MapField(intersectKey)] = Map(destinationData);
                    }
                    else
                    {
                        diffSource[MapField(intersectKey)] = sourceData;
                        diffDestination[MapField(intersectKey)] = destinationData;
                    }
                }
            }
            #region obsolete
            //foreach (var sourcePair in source.Data)
            //{
            //    if (destination.Data.TryGetValue(sourcePair.Key, out object destinationData))  //same key/field
            //    {
            //        if (sourcePair.Value is IComparable)
            //        {
            //            if (((IComparable)sourcePair.Value).CompareTo(destinationData) != 0)    //different value
            //            {
            //                diffSource[sourcePair.Key] = sourcePair.Value;
            //                diffDestination[sourcePair.Key] = destinationData;

            //                containKeys.Add(sourcePair.Key);
            //            }
            //        }
            //        else if (!sourcePair.Value.Equals(destinationData)) //different value
            //        {
            //            diffSource[sourcePair.Key] = sourcePair.Value;
            //            diffDestination[sourcePair.Key] = destinationData;

            //            containKeys.Add(sourcePair.Key);
            //        }
            //    }
            //    else
            //    {   //source has a key but destination no such key
            //        diffSource[sourcePair.Key] = sourcePair.Value;
            //        diffDestination[sourcePair.Key] = destinationData;   //destinationData = null

            //        containKeys.Add(sourcePair.Key);
            //    }
            //}

            //foreach (var destinationPair in destination.Data)
            //{
            //    if (!containKeys.Contains(destinationPair.Key))
            //    {
            //        if (source.Data.TryGetValue(destinationPair.Key, out object sourceData))  //same key/field
            //        {
            //            if (destinationPair.Value is IComparable)
            //            {
            //                if (((IComparable)destinationPair.Value).CompareTo(sourceData) != 0)    //different value
            //                {
            //                    diffDestination[destinationPair.Key] = destinationPair.Value;
            //                    diffSource[destinationPair.Key] = sourceData;

            //                    containKeys.Add(destinationPair.Key);
            //                }
            //            }
            //            else if (!destinationPair.Value.Equals(sourceData)) //different value
            //            {
            //                diffDestination[destinationPair.Key] = destinationPair.Value;
            //                diffSource[destinationPair.Key] = sourceData;

            //                containKeys.Add(destinationPair.Key);
            //            }
            //        }
            //        else
            //        {   //destination has a key but source no such key
            //            diffDestination[destinationPair.Key] = destinationPair.Value;
            //            diffSource[destinationPair.Key] = sourceData;   //sourceData = null

            //            containKeys.Add(destinationPair.Key);
            //        }
            //    }
            //}
            #endregion
            return compareResult;
        }
    }
}
