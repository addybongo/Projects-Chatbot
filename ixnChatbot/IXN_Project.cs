using System;
using System.Collections.Generic;

namespace ixnChatbot
{
    public class IXN_Project : Project
    {
        private readonly Dictionary<string, int> moreFields;

        public IXN_Project(string[] fields, string[] values)
        :base(fields, values, true)
        {
            moreFields = new Dictionary<string, int>();
            this.values = values;

            int fieldsInFirstTable = this.fields.Count;
            
            //All the fields that were missed in the Project constructor are added in here
            for (int i = fieldsInFirstTable; i < fields.Length; i++)
            {
                moreFields.Add(fields[i], i);
            }
        }
    }
}