using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Helper.Data
{
    public class DbCompare
    {
        private Dictionary<string, string> DictMatchFields { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Func<object, object>> DictMapFields = new Dictionary<string, Func<object, object>>();
        private List<string> ignoredColumns = new List<string>();

        public DbCompare(Dictionary<string, string> dictMatchFields = null)
        {
            this.DictMatchFields = dictMatchFields;
        }

        public void IgnoreProperty(params string[] propertyName)
        {
            this.ignoredColumns.AddRange(propertyName);
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

        public Tuple<DbModel, DbModel> CompareDbModels(DbModel source, DbModel destination)
        {
            //remove all ignored key.
            if (source != null)
            {
                foreach (var key in source.Data.Keys.ToArray().Intersect(this.ignoredColumns))
                    source.Data.Remove(key);
            }

            if (destination != null)
            {
                foreach (var key in destination.Data.Keys.ToArray().Intersect(this.ignoredColumns))
                    destination.Data.Remove(key);
            }


            var diffSource = new DbModel();
            var diffDestination = new DbModel();

            Tuple<DbModel, DbModel> compareResult = new Tuple<DbModel, DbModel>(diffSource, diffDestination);
            //left to right compare
            if (source != null)
            {
                var sourceKeys = source.Data.Keys.Select(a => a.ToString());

                if (destination != null)
                    sourceKeys = sourceKeys.Except(destination.Data.Keys);    //source has desti no

                foreach (var sourceKey in sourceKeys)
                {
                    var sourceData = source[sourceKey];
                    addDiff(sourceKey, sourceData, null);
                    /*
                    if (DictMapFields.TryGetValue(sourceKey, out Func<object, object> Map))
                    {
                        diffSource[MapField(sourceKey)] = Map(sourceData);
                        diffDestination[MapField(sourceKey)] = null;    //destinationData = null
                    }
                    else
                    {
                        diffSource[MapField(sourceKey)] = sourceData;
                        diffDestination[MapField(sourceKey)] = null;
                    }*/
                }
            }

            //right to left compare
            if (destination != null)
            {
                var xDestinationKeys = destination.Data.Keys.Select(a => a.ToString());
                if (source != null)
                    xDestinationKeys = xDestinationKeys.Except(source.Data.Keys, StringComparer.OrdinalIgnoreCase);  //desti has source no

                foreach (var xDestinationKey in xDestinationKeys)
                {
                    var destinationData = destination[xDestinationKey];
                    addDiff(xDestinationKey, null, destinationData);
                    /*
                    if (DictMapFields.TryGetValue(xDestinationKey, out Func<object, object> Map))
                    {
                        diffDestination[MapField(xDestinationKey)] = Map(destinationData);
                        diffSource[MapField(xDestinationKey)] = null;    //destinationData = null
                    }
                    else
                    {
                        diffDestination[MapField(xDestinationKey)] = destinationData;
                        diffSource[MapField(xDestinationKey)] = null;
                    }*/
                }
            }

            //cross compare for same fields
            if (source != null && destination != null)
            {
                var intersectKeys = source.Data.Keys.Intersect(destination.Data.Keys);
                foreach (var intersectKey in intersectKeys)
                {
                    var sourceData = source[intersectKey];
                    var destinationData = destination[intersectKey];
                    if (isDiff(sourceData, destinationData))
                    {
                        addDiff(intersectKey, sourceData, destinationData);
                    }
                    //cater for null value.
                    /*
                    if(sourceData == null && destinationData != null)
                    {
                        addDiff(intersectKey, sourceData, destinationData);
                        diffSource[MapField(intersectKey)] = null;
                        diffDestination[MapField(intersectKey)] = Map(destinationData);
                    }
                    else if(sourceData != null && destinationData == null)
                    {

                    }
                    else if (sourceData is IComparable)
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
                    } */
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

            bool isDiff(object sourceVal, object destVal)
            {
                if (sourceVal == null && destVal != null)
                    return true;
                if (destVal == null && sourceVal != null)
                    return true;
                if (destVal == null && sourceVal == null)
                    return false;
                if (sourceVal is IComparable)
                    return (((IComparable)sourceVal).CompareTo(destVal) != 0);
                return !sourceVal.Equals(destVal);
            }

            void addDiff(string key, object sourceVal, object destVal)
            {
                var hasMap = DictMapFields.TryGetValue(key, out Func<object, object> Map);
                if (hasMap && sourceVal != null)
                    diffSource[MapField(key)] = Map(sourceVal);
                else
                    diffSource[MapField(key)] = sourceVal;

                if (hasMap && destVal != null)
                    diffDestination[MapField(key)] = Map(destVal);
                else
                    diffDestination[MapField(key)] = destVal;
            }
        }
    }
}
