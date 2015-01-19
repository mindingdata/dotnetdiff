using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDiff
{
    public static class DotNetDiff
    {
        public static IEnumerable<DiffOperation> GenerateDiff(object originalDocument, object modifiedDocument, string path = "/")
        {
            if (originalDocument != null && modifiedDocument != null && originalDocument.GetType() != modifiedDocument.GetType())
                throw new ArgumentException(string.Format("Original Document is type of {0} but Modified Document is of type {1}", originalDocument.GetType(), modifiedDocument.GetType()));


            //If it's just a value type or a string, then we can compare them here without going any further. 
            if (originalDocument.GetType().GetGenericArguments().Any(t => t.IsValueType && t.IsPrimitive) || originalDocument is string)
            {
                if (originalDocument != modifiedDocument)
                {
                    yield return new DiffOperation()
                    {
                        Operation = modifiedDocument == null ? DiffOperationType.Remove : DiffOperationType.Replace,
                        Path = path,
                        Value = modifiedDocument
                    };
                }
                yield break;
            }

            var propertyList = originalDocument.GetType().GetProperties();
            foreach (var property in propertyList)
            {
                var originalValue = property.GetValue(originalDocument);
                var modifiedValue = property.GetValue(modifiedDocument);

                //If both values are null. Just continue because it saves a bit of hassle later on
                if (originalValue == null && modifiedValue == null)
                    continue;


                //If one of the values is null, then we don't need to do a deep check. 
                if (originalValue == null || modifiedValue == null)
                {
                    yield return new DiffOperation
                    {
                        Operation = modifiedValue == null ? DiffOperationType.Remove : DiffOperationType.Add,
                        Path = path + property.Name,
                        Value = modifiedValue
                    };
                    continue;
                }

                //If it's an array. 
                if (property.PropertyType.IsArray || (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    var originalArrayValue = property.PropertyType.IsArray ? originalValue as Array : ((IEnumerable)originalValue).Cast<object>().ToArray();
                    var modifiedArrayValue = property.PropertyType.IsArray ? modifiedValue as Array : ((IEnumerable)modifiedValue).Cast<object>().ToArray();

                    //Array add/remove
                    foreach (var addRemoveDiff in ArrayAddRemoveDiff(originalArrayValue, modifiedArrayValue, path + property.Name + "/"))
                        yield return addRemoveDiff;

                    //Compare each object in the array recursively. 
                    var compareCount = Math.Min(originalArrayValue.Length, modifiedArrayValue.Length);
                    for (int i = 0; i < compareCount; i++)
                    {
                        foreach (var itemDiff in GenerateDiff(originalArrayValue.GetValue(i), modifiedArrayValue.GetValue(i), path + property.Name + "/" + i))
                            yield return itemDiff;
                    }

                }
                //Iif it's a non value type (e.g. a nested class)
                else if (!property.PropertyType.GetGenericArguments().Any(t => t.IsValueType && t.IsPrimitive) && property.PropertyType != typeof(string))
                {
                    foreach (var patchOperation in GenerateDiff(originalValue, modifiedValue, path + property.Name + "/"))
                        yield return patchOperation;
                }
                //it's a value type at this point, just compare values. 
                else if (originalValue != modifiedValue)
                {
                    //Standard Value Type/String
                    yield return new DiffOperation()
                    {
                        Operation = DiffOperationType.Replace,
                        Path = path + property.Name,
                        Value = property.GetValue(modifiedDocument)
                    };
                }
            }
        }

        private static IEnumerable<DiffOperation> ArrayAddRemoveDiff(Array originalArray, Array modifiedArray, string path)
        {
            var lengthDifference = Math.Abs(originalArray.Length - modifiedArray.Length);

            var count = 0;
            while (count < lengthDifference)
            {
                if (originalArray.Length > modifiedArray.Length)
                {
                    yield return new DiffOperation
                    {
                        Operation = DiffOperationType.Remove,
                        Path = path + (originalArray.Length - 1 - count)
                    };
                }
                else
                {
                    yield return new DiffOperation()
                    {
                        Operation = DiffOperationType.Add,
                        Path = path + (originalArray.Length + count),
                        Value = modifiedArray.GetValue(originalArray.Length + count)
                    };
                }
                count++;
            }
        }
    }

}