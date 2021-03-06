/*******************************************************************************
* Copyright (C) 2012 eBay Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
******************************************************************************/

using System.Dynamic;
using System.Text;

namespace AsimovDeploy.WinAgent.Framework.Models
{
    public class PasswordActionParameter : ActionParameter
    {
        public string Password { get; set; }
        public string Default { get; set; }

        public override dynamic GetDescriptor()
        {
            dynamic descriptor = new ExpandoObject();
            descriptor.type = "password";
            descriptor.name = Name;
            descriptor.@default = Default;
            return descriptor;
        }

        public override void ApplyToPowershellScript(StringBuilder script, dynamic value)
        {
            if (!string.IsNullOrEmpty(Password))
            {
                return;
            }

            var scriptToInsert = string.Format("${0} = \"{1}\"\n", Name, value);
            script.Insert(0, scriptToInsert);
        }
    }
}