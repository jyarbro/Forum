﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CodeKicker.BBCode {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///    A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resource {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        internal Resource() {
        }
        
        /// <summary>
        ///    Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CodeKicker.BBCode.Resource", typeof(Resource).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///    Overrides the current thread's CurrentUICulture property for all
        ///    resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The tag {0} has a duplicate attribute {1}..
        /// </summary>
        public static string DuplicateAttribute {
            get {
                return ResourceManager.GetString("DuplicateAttribute", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The character &quot;\&quot; is used to escape &quot;]&quot; and &quot;[&quot;. Please escape it like this &quot;\\&quot;..
        /// </summary>
        public static string EscapeChar {
            get {
                return ResourceManager.GetString("EscapeChar", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The tag {0} cannot have a value..
        /// </summary>
        public static string MissingDefaultAttribute {
            get {
                return ResourceManager.GetString("MissingDefaultAttribute", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to In the tag {0} no attributes are allowed..
        /// </summary>
        public static string NoAttributesAllowed {
            get {
                return ResourceManager.GetString("NoAttributesAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The character &quot;]&quot; cannot be used in text or code without beeing escaped. Please write &quot;\]&quot; instead..
        /// </summary>
        public static string NonescapedChar {
            get {
                return ResourceManager.GetString("NonescapedChar", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The tag {0} was not closed correctly..
        /// </summary>
        public static string TagNotClosed {
            get {
                return ResourceManager.GetString("TagNotClosed", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The end-tag {0} does not match the preceding start-tag {1}..
        /// </summary>
        public static string TagNotMatching {
            get {
                return ResourceManager.GetString("TagNotMatching", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The tag {0} has not been closed..
        /// </summary>
        public static string TagNotOpened {
            get {
                return ResourceManager.GetString("TagNotOpened", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The tag {0} does not have an attribute {1}..
        /// </summary>
        public static string UnknownAttribute {
            get {
                return ResourceManager.GetString("UnknownAttribute", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The tag {0} does not exists..
        /// </summary>
        public static string UnknownTag {
            get {
                return ResourceManager.GetString("UnknownTag", resourceCulture);
            }
        }
    }
}
