﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Cave.Logging.Properties {
    using System;
    
    
    /// <summary>
    ///   Eine stark typisierte Ressourcenklasse zum Suchen von lokalisierten Zeichenfolgen usw.
    /// </summary>
    // Diese Klasse wurde von der StronglyTypedResourceBuilder automatisch generiert
    // -Klasse über ein Tool wie ResGen oder Visual Studio automatisch generiert.
    // Um einen Member hinzuzufügen oder zu entfernen, bearbeiten Sie die .ResX-Datei und führen dann ResGen
    // mit der /str-Option erneut aus, oder Sie erstellen Ihr VS-Projekt neu.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
       static global::System.Resources.ResourceManager resourceMan;
        
       static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Gibt die zwischengespeicherte ResourceManager-Instanz zurück, die von dieser Klasse verwendet wird.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Cave.Logging.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Überschreibt die CurrentUICulture-Eigenschaft des aktuellen Threads für alle
        ///   Ressourcenzuordnungen, die diese stark typisierte Ressourcenklasse verwenden.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {0} encountered a fatal exception! ähnelt.
        /// </summary>
        internal static string Error_FatalExceptionAt {
            get {
                return ResourceManager.GetString("Error_FatalExceptionAt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &lt;script&gt;function dean_addEvent(a,b,c){if(a.addEventListener)a.addEventListener(b,c,!1);else{c.$$guid||(c.$$guid=dean_addEvent.guid++),a.events||(a.events={});var d=a.events[b];d||(d=a.events[b]={},a[&quot;on&quot;+b]&amp;&amp;(d[0]=a[&quot;on&quot;+b])),d[c.$$guid]=c,a[&quot;on&quot;+b]=handleEvent}}function removeEvent(a,b,c){a.removeEventListener?a.removeEventListener(b,c,!1):a.events&amp;&amp;a.events[b]&amp;&amp;delete a.events[b][c.$$guid]}function handleEvent(a){var b=!0;a=a||fixEvent(((this.ownerDocument||this.document||this).parentWindow||window).event [Rest der Zeichenfolge wurde abgeschnitten]&quot;; ähnelt.
        /// </summary>
        internal static string LogHtmsortTable {
            get {
                return ResourceManager.GetString("LogHtmsortTable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &lt;style&gt;table.sortable thead{background-color:#eee;color:#333;font-weight:bold;cursor:pointer}table.sortable tr td{background-color:#333;color:#eee;vertical-align:top}table.sortable tr td.Invalid{color:Invalid}table.sortable tr td.Black{color:Black}table.sortable tr td.Gray{color:Gray}table.sortable tr td.Blue{color:Blue}table.sortable tr td.Green{color:Green}table.sortable tr td.Cyan{color:Cyan}table.sortable tr td.Red{color:Red}table.sortable tr td.Magenta{color:Magenta}table.sortable tr td.Yellow{color:Ye [Rest der Zeichenfolge wurde abgeschnitten]&quot;; ähnelt.
        /// </summary>
        internal static string LogHtmstyle {
            get {
                return ResourceManager.GetString("LogHtmstyle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die LogReceiver {0} has a backlog of {1} messages (current delay {2})! ähnelt.
        /// </summary>
        internal static string LogReceiver_Backlog {
            get {
                return ResourceManager.GetString("LogReceiver_Backlog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die LogReceiver {0} backlog has recovered! ähnelt.
        /// </summary>
        internal static string LogReceiver_BacklogRecovered {
            get {
                return ResourceManager.GetString("LogReceiver_BacklogRecovered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die LogReceiver {0} discarded {1} late messages! ähnelt.
        /// </summary>
        internal static string LogReceiver_Discarded {
            get {
                return ResourceManager.GetString("LogReceiver_Discarded", resourceCulture);
            }
        }
    }
}
