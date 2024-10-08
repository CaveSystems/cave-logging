﻿using System;
using System.IO;

namespace Cave.Logging;

/// <summary>Use this class to write messages directly to a html logfile.</summary>
/// <seealso cref="LogFileBase"/>
public class LogHtmlFile : LogFileBase
{
    #region Private Fields

    StreamWriter? writer;

    #endregion Private Fields

    #region Public Classes

    /// <summary>Provides header descriptions</summary>
    public class TableHeaderDescriptions
    {
        #region Public Properties

        /// <summary>Gets or sets the content header</summary>
        public string Content { get; set; } = "Content";

        /// <summary>Gets or sets the datetime header</summary>
        public string DateTime { get; set; } = "DateTime";

        /// <summary>Gets or sets the file header</summary>
        public string File { get; set; } = "File";

        /// <summary>Gets or sets the level header</summary>
        public string Level { get; set; } = "Level";

        /// <summary>Gets or sets the sender header</summary>
        public string Sender { get; set; } = "Sender";

        #endregion Public Properties
    }

    #endregion Public Classes

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="LogHtmlFile"/> class.</summary>
    /// <param name="fileName">Name of the file.</param>
    /// <remarks>This class does not automatically start writing to the file, use <see cref="Start"/> to begin output.</remarks>
    public LogHtmlFile(string fileName)
        : base(fileName)
    {
        if (!fileName.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase)) fileName += FileExtension;
        if (File.Exists(fileName))
        {
            var ext = Path.GetExtension(fileName);
            var folder = Path.GetDirectoryName(fileName) ?? ".";
            var moveFileName = Path.Combine(folder, Path.GetFileNameWithoutExtension(fileName) + "." + DateTime.Now.ToString(StringExtensions.FileNameDateTimeFormat) + ext);
            File.Move(fileName, moveFileName);
        }
        MessageFormatter.MessageFormat = "{Content}";
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets or sets the default extension for all new instances.</summary>
    public static string FileExtension { get; set; } = ".html";

    /// <summary>Gets or sets the document style</summary>
    public string DocumentStyle { get; set; } = "<style>table.sortable th{background-color:#eee;color:#333;font-weight:bold;cursor:pointer}tr{background-color:#333;color:#eee;vertical-align:top}" +
        //cells
        "td.BgBlack{color:Gray;background-color:Black}td.BgGray{color:White;background-color:DarkGray}td.BgBlue{color:White;background-color:DarkBlue}td.BgGreen{color:White;background-color:DarkGreen}" +
        "td.BgCyan{color:White;background-color:DarkCyan}td.BgRed{color:White;background-color:DarkRed}td.BgMagenta{color:White;background-color:DarkMagenta}td.BgYellow{color:White;background-color:Orange}" +
        "td.BgWhite{color:White;background-color:Gray}" +
        //spans
        "span.Black{color:Black}span.Gray{color:LightGray}span.Blue{color:LightBlue}span.Green{color:LightGreen}span.Cyan{color:Cyan}span.Red{color:Red}span.Magenta{color:LightMagenta}span.Yellow{color:Yellow}" +
        "span.White{color:White}table.sortable{border:1px solid #777}</style>";

    /// <summary>Gets the table header descriptions used.</summary>
    public TableHeaderDescriptions Header { get; } = new();

    /// <summary>Gets or sets the sort table script</summary>
    public string SortTableScript { get; set; } = "<script>function dean_addEvent(a,b,c){if(a.addEventListener)a.addEventListener(b,c,!1);else{c.$$guid||(c.$$guid=dean_addEvent.guid++),a.events||(a.events={});var d=a.events[b];d||(d=a.events[b]={},a[\"on\"+b]&&(d[0]=a[\"on\"+b])),d[c.$$guid]=c,a[\"on\"+b]=handleEvent}}function removeEvent(a,b,c){a.removeEventListener?a.removeEventListener(b,c,!1):a.events&&a.events[b]&&delete a.events[b][c.$$guid]}function handleEvent(a){var b=!0;a=a||fixEvent(((this.ownerDocument||this.document||this).parentWindow||window).event);var c=this.events[a.type];for(var d in c)this.$$handleEvent=c[d],this.$$handleEvent(a)===!1&&(b=!1);return b}function fixEvent(a){return a.preventDefault=fixEvent.preventDefault,a.stopPropagation=fixEvent.stopPropagation,a}var stIsIE=!1;if(sorttable={init:function(){arguments.callee.done||(arguments.callee.done=!0,_timer&&clearInterval(_timer),document.createElement&&document.getElementsByTagName&&(sorttable.DATE_RE=/^(\\d\\d?)[\\/\\.-](\\d\\d?)[\\/\\.-]((\\d\\d)?\\d\\d)$/,forEach(document.getElementsByTagName(\"table\"),function(a){a.className.search(/\\bsortable\\b/)!=-1&&sorttable.makeSortable(a)})))},makeSortable:function(a){if(0==a.getElementsByTagName(\"thead\").length&&(the=document.createElement(\"thead\"),the.appendChild(a.rows[0]),a.insertBefore(the,a.firstChild)),null==a.tHead&&(a.tHead=a.getElementsByTagName(\"thead\")[0]),1==a.tHead.rows.length){sortbottomrows=[];for(var b=0;b<a.rows.length;b++)a.rows[b].className.search(/\\bsortbottom\\b/)!=-1&&(sortbottomrows[sortbottomrows.length]=a.rows[b]);if(sortbottomrows){null==a.tFoot&&(tfo=document.createElement(\"tfoot\"),a.appendChild(tfo));for(var b=0;b<sortbottomrows.length;b++)tfo.appendChild(sortbottomrows[b]);delete sortbottomrows}headrow=a.tHead.rows[0].cells;for(var b=0;b<headrow.length;b++)headrow[b].className.match(/\\bsorttable_nosort\\b/)||(mtch=headrow[b].className.match(/\\bsorttable_([a-z0-9]+)\\b/),mtch&&(override=mtch[1]),mtch&&\"function\"==typeof sorttable[\"sort_\"+override]?headrow[b].sorttable_sortfunction=sorttable[\"sort_\"+override]:headrow[b].sorttable_sortfunction=sorttable.guessType(a,b),headrow[b].sorttable_columnindex=b,headrow[b].sorttable_tbody=a.tBodies[0],dean_addEvent(headrow[b],\"click\",sorttable.innerSortFunction=function(a){if(this.className.search(/\\bsorttable_sorted\\b/)!=-1)return sorttable.reverse(this.sorttable_tbody),this.className=this.className.replace(\"sorttable_sorted\",\"sorttable_sorted_reverse\"),this.removeChild(document.getElementById(\"sorttable_sortfwdind\")),sortrevind=document.createElement(\"span\"),sortrevind.id=\"sorttable_sortrevind\",sortrevind.innerHTML=stIsIE?'&nbsp<font face=\"webdings\">5</font>':\"&nbsp;&#x25B4;\",void this.appendChild(sortrevind);if(this.className.search(/\\bsorttable_sorted_reverse\\b/)!=-1)return sorttable.reverse(this.sorttable_tbody),this.className=this.className.replace(\"sorttable_sorted_reverse\",\"sorttable_sorted\"),this.removeChild(document.getElementById(\"sorttable_sortrevind\")),sortfwdind=document.createElement(\"span\"),sortfwdind.id=\"sorttable_sortfwdind\",sortfwdind.innerHTML=stIsIE?'&nbsp<font face=\"webdings\">6</font>':\"&nbsp;&#x25BE;\",void this.appendChild(sortfwdind);theadrow=this.parentNode,forEach(theadrow.childNodes,function(a){1==a.nodeType&&(a.className=a.className.replace(\"sorttable_sorted_reverse\",\"\"),a.className=a.className.replace(\"sorttable_sorted\",\"\"))}),sortfwdind=document.getElementById(\"sorttable_sortfwdind\"),sortfwdind&&sortfwdind.parentNode.removeChild(sortfwdind),sortrevind=document.getElementById(\"sorttable_sortrevind\"),sortrevind&&sortrevind.parentNode.removeChild(sortrevind),this.className+=\" sorttable_sorted\",sortfwdind=document.createElement(\"span\"),sortfwdind.id=\"sorttable_sortfwdind\",sortfwdind.innerHTML=stIsIE?'&nbsp<font face=\"webdings\">6</font>':\"&nbsp;&#x25BE;\",this.appendChild(sortfwdind),row_array=[],col=this.sorttable_columnindex,rows=this.sorttable_tbody.rows;for(var b=0;b<rows.length;b++)row_array[row_array.length]=[sorttable.getInnerText(rows[b].cells[col]),rows[b]];row_array.sort(this.sorttable_sortfunction),tb=this.sorttable_tbody;for(var b=0;b<row_array.length;b++)tb.appendChild(row_array[b][1]);delete row_array}))}},guessType:function(a,b){sortfn=sorttable.sort_alpha;for(var c=0;c<a.tBodies[0].rows.length;c++)if(text=sorttable.getInnerText(a.tBodies[0].rows[c].cells[b]),\"\"!=text){if(text.match(/^-?[£$¤]?[\\d,.]+%?$/))return sorttable.sort_numeric;if(possdate=text.match(sorttable.DATE_RE),possdate){if(first=parseInt(possdate[1]),second=parseInt(possdate[2]),first>12)return sorttable.sort_ddmm;if(second>12)return sorttable.sort_mmdd;sortfn=sorttable.sort_ddmm}}return sortfn},getInnerText:function(a){if(!a)return\"\";if(hasInputs=\"function\"==typeof a.getElementsByTagName&&a.getElementsByTagName(\"input\").length,null!=a.getAttribute(\"sorttable_customkey\"))return a.getAttribute(\"sorttable_customkey\");if(\"undefined\"!=typeof a.textContent&&!hasInputs)return a.textContent.replace(/^\\s+|\\s+$/g,\"\");if(\"undefined\"!=typeof a.innerText&&!hasInputs)return a.innerText.replace(/^\\s+|\\s+$/g,\"\");if(\"undefined\"!=typeof a.text&&!hasInputs)return a.text.replace(/^\\s+|\\s+$/g,\"\");switch(a.nodeType){case 3:if(\"input\"==a.nodeName.toLowerCase())return a.value.replace(/^\\s+|\\s+$/g,\"\");case 4:return a.nodeValue.replace(/^\\s+|\\s+$/g,\"\");case 1:case 11:for(var b=\"\",c=0;c<a.childNodes.length;c++)b+=sorttable.getInnerText(a.childNodes[c]);return b.replace(/^\\s+|\\s+$/g,\"\");default:return\"\"}},reverse:function(a){newrows=[];for(var b=0;b<a.rows.length;b++)newrows[newrows.length]=a.rows[b];for(var b=newrows.length-1;b>=0;b--)a.appendChild(newrows[b]);delete newrows},sort_numeric:function(a,b){return aa=parseFloat(a[0].replace(/[^0-9.-]/g,\"\")),isNaN(aa)&&(aa=0),bb=parseFloat(b[0].replace(/[^0-9.-]/g,\"\")),isNaN(bb)&&(bb=0),aa-bb},sort_alpha:function(a,b){return a[0]==b[0]?0:a[0]<b[0]?-1:1},sort_ddmm:function(a,b){return mtch=a[0].match(sorttable.DATE_RE),y=mtch[3],m=mtch[2],d=mtch[1],1==m.length&&(m=\"0\"+m),1==d.length&&(d=\"0\"+d),dt1=y+m+d,mtch=b[0].match(sorttable.DATE_RE),y=mtch[3],m=mtch[2],d=mtch[1],1==m.length&&(m=\"0\"+m),1==d.length&&(d=\"0\"+d),dt2=y+m+d,dt1==dt2?0:dt1<dt2?-1:1},sort_mmdd:function(a,b){return mtch=a[0].match(sorttable.DATE_RE),y=mtch[3],d=mtch[2],m=mtch[1],1==m.length&&(m=\"0\"+m),1==d.length&&(d=\"0\"+d),dt1=y+m+d,mtch=b[0].match(sorttable.DATE_RE),y=mtch[3],d=mtch[2],m=mtch[1],1==m.length&&(m=\"0\"+m),1==d.length&&(d=\"0\"+d),dt2=y+m+d,dt1==dt2?0:dt1<dt2?-1:1},shaker_sort:function(a,b){for(var c=0,d=a.length-1,e=!0;e;){e=!1;for(var f=c;f<d;++f)if(b(a[f],a[f+1])>0){var g=a[f];a[f]=a[f+1],a[f+1]=g,e=!0}if(d--,!e)break;for(var f=d;f>c;--f)if(b(a[f],a[f-1])<0){var g=a[f];a[f]=a[f-1],a[f-1]=g,e=!0}c++}}},document.addEventListener&&document.addEventListener(\"DOMContentLoaded\",sorttable.init,!1),/WebKit/i.test(navigator.userAgent))var _timer=setInterval(function(){/loaded|complete/.test(document.readyState)&&sorttable.init()},10);window.onload=sorttable.init,dean_addEvent.guid=1,fixEvent.preventDefault=function(){this.returnValue=!1},fixEvent.stopPropagation=function(){this.cancelBubble=!0},Array.forEach||(Array.forEach=function(a,b,c){for(var d=0;d<a.length;d++)b.call(c,a[d],d,a)}),Function.prototype.forEach=function(a,b,c){for(var d in a)\"undefined\"==typeof this.prototype[d]&&b.call(c,a[d],d,a)},String.forEach=function(a,b,c){Array.forEach(a.split(\"\"),function(d,e){b.call(c,d,e,a)})};var forEach=function(a,b,c){if(a){var d=Object;if(a instanceof Function)d=Function;else{if(a.forEach instanceof Function)return void a.forEach(b,c);\"string\"==typeof a?d=String:\"number\"==typeof a.length&&(d=Array)}d.forEach(a,b,c)}};</script>";

    #endregion Public Properties

    #region Public Methods

    /// <summary>Starts a <see cref="LogHtmlFile"/> instance for the local machine.</summary>
    public static LogHtmlFile StartLocalMachineLogFile(LogFileFlags flags, string? additionalPath = null) => Start(new LogHtmlFile(GetLocalMachineLogFileName(flags, additionalPath) + FileExtension));

    /// <summary>Starts a <see cref="LogHtmlFile"/> instance for the local user.</summary>
    public static LogHtmlFile StartLocalUserLogFile(LogFileFlags flags, string? additionalPath = null) => Start(new LogHtmlFile(GetLocalUserLogFileName(flags, additionalPath) + FileExtension));

    /// <summary>Starts a new instance of the <see cref="LogHtmlFile"/> class.</summary>
    /// <param name="fileName">Filename to log to</param>
    /// <returns>Returns a new instance</returns>
    public static LogHtmlFile StartLogFile(string fileName) => Start(new LogHtmlFile(fileName));

    /// <summary>
    /// Starts a <see cref="LogHtmlFile"/> instance for the current running program in the programs startup directory. This should only be used for
    /// administration processes. Attention do nut use this for service processes!.
    /// </summary>
    /// <returns></returns>
    public static LogHtmlFile StartProgramLogFile(LogFileFlags flags, string? additionalPath = null) => Start(new LogHtmlFile(GetProgramLogFileName(flags, additionalPath) + FileExtension));

    /// <summary>Starts a <see cref="LogHtmlFile"/> instance for the current (roaming) user.</summary>
    public static LogHtmlFile StartUserLogFile(LogFileFlags flags, string? additionalPath = null) => Start(new LogHtmlFile(GetUserLogFileName(flags, additionalPath) + FileExtension));

    /// <summary>Closes the <see cref="LogReceiver"/>.</summary>
    public override void Close()
    {
        lock (this)
        {
            if (writer != null)
            {
                writer.WriteLine("</table></body></html>");
                writer.Close();
                writer = null;
            }
        }
    }

    /// <summary>Starts writing to the output stream.</summary>
    public override void Start()
    {
        var stream = File.Create(FileName);
        writer = new StreamWriter(stream);
        writer.WriteLine("<html><head>");
        writer.WriteLine(SortTableScript);
        writer.WriteLine(DocumentStyle);
        writer.WriteLine("</head>");
        writer.WriteLine("<body style=\"color:#eee;background-color:#555;font-family:monospace;\"><table class=\"sortable\">");
        writer.WriteLine($"<tr><th>{Header.DateTime}</th><th>{Header.Level}</th><th>{Header.Sender}</th><th>{Header.File}</th><th>{Header.Content}</th></tr>");
        writer.Flush();
        base.Start();
    }

    /// <inheritdoc/>
    public override void Write(LogMessage message)
    {
        if (writer == null) return;

        // start row
        var levelColor = message.Level.GetLogLevelColor();
        writer.Write("<tr>");

        // datetime
        writer.Write("<td>" + message.DateTime.ToString(MessageFormatter.DateTimeFormat) + "</td>");

        // loglevel
        writer.Write($"<td class=\"Bg{levelColor}\">{message.Level}</td>");

        // sender
        writer.Write("<td>");
        writer.Write(message.SenderName);
        writer.Write("</td>");

        //file
        writer.Write("<td>");
        writer.Write(Path.GetFileName(message.SourceFile));
        if (message.SourceLine > 0) writer.Write($"({message.SourceLine})");
        writer.Write("</td>");

        // colored content
        {
            writer.Write("<td>");
            var color = (LogColor)0;
            var items = MessageFormatter.FormatMessage(message);
            foreach (var item in items)
            {
                if (item.Color != color)
                {
                    if (color != 0)
                    {
                        writer.Write("</span>");
                    }

                    color = item.Color;
                    if (color != 0)
                    {
                        writer.Write($"<span class=\"{color}\">");
                    }
                }

                if (item.Equals(LogText.NewLine))
                {
                    writer.Write("<br/>");
                }
                else
                {
                    writer.Write(item.Text);
                }
            }

            if (color != 0)
            {
                writer.Write("</span>");
            }

            writer.Write("</td>");
        }

        // end row
        writer.WriteLine("</tr>");
        writer.Flush();
    }

    #endregion Public Methods
}
