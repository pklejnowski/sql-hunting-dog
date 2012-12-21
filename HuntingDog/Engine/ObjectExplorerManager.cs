using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using HuntingDog;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
//using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Reflection;
using System.Windows;
//using EnvDTE100;
//using EnvDTE;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections;

namespace DatabaseObjectSearcher
{
    public class ObjectExplorerManager
    {

        //public  List<NavigatorServer> GetServers()
        //{
        //    var r = new List<NavigatorServer>();
        //    foreach (var srvConnectionInfo in GetAllServers())
        //    {
        //        var nvServer = new NavigatorServer(srvConnectionInfo, srvConnectionInfo.ServerName);
        //        r.Add(nvServer);
        //    }
        //    return r;
        //}

        private static IObjectExplorerService _objExplorer = null;
        public IObjectExplorerService GetObjectExplorer()
        {
            if(_objExplorer==null)
                _objExplorer = (IObjectExplorerService)ServiceCache.ServiceProvider.GetService(typeof(IObjectExplorerService));
            return _objExplorer;
        }

        private static bool? _is2008R2 = null;
        public static bool Is2008R2
        {
            get
            {
                if(_is2008R2==null)
                {
                    _is2008R2 = true;
                    try
                    {
                        var eventProvider = Assembly.Load("SqlWorkbench.Interfaces").GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.IObjectExplorerEventProvider");
                        if(eventProvider!=null)
                            _is2008R2 = false;
                    }
                    catch (Exception)
                    {
                        // if exception happned then we decide that it is 2008
                    }
                }

                return _is2008R2.Value;
    
            }
        }


        void SetNON2008R2ObjectExplorerEventProvider()
        {
            // the old way of doing things
            //IObjectExplorerEventProvider objectExplorer = (IObjectExplorerEventProvider)Common.ObjectExplorerService.GetService(typeof(IObjectExplorerEventProvider));
            //objectExplorer.SelectionChanged += new NodesChangedEventHandler(objectExplorer_SelectionChanged);
            var t = Assembly.Load("SqlWorkbench.Interfaces").GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.IObjectExplorerEventProvider");


            MethodInfo mi = this.GetType().GetMethod("Provider_SelectionChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // get the IObjectExplorerEventProvider from the ObjectExplorerService
            object objectExplorer = GetObjectExplorer().GetService(t);
            EventInfo ei = t.GetEvent("SelectionChanged", System.Reflection.BindingFlags.Public | BindingFlags.Instance);
            // use this overload CreateDelegate(Type type, object firstArgument, MethodInfo method);
            // the 2nd param is "this" because the method to handle the event is in it.
            Delegate del = Delegate.CreateDelegate(ei.EventHandlerType, this, mi);
            ei.AddEventHandler(objectExplorer, del);
        }

        void Set2008R2ObjectExplorerEventProvider()
        {
            // the old way of doing things
            //Microsoft.SqlServer.Management.SqlStudio.Explorer.ObjectExplorerService objectExplorer = Common.ObjectExplorerService as Microsoft.SqlServer.Management.SqlStudio.Explorer.ObjectExplorerService;
            //int nodeCount;
            //INodeInformation[] nodes;
            //objectExplorer.GetSelectedNodes(out nodeCount, out nodes);
            //Microsoft.SqlServer.Management.SqlStudio.Explorer.ContextService contextService = (Microsoft.SqlServer.Management.SqlStudio.Explorer.ContextService)objectExplorer.Container.Components[1];
            //// or ContextService contextService = (ContextService)objectExplorer.Site.Container.Components[1];
            //INavigationContextProvider provider = contextService.ObjectExplorerContext;
            //provider.CurrentContextChanged += new NodesChangedEventHandler(ObjectExplorer_SelectionChanged);

            System.Type t = Assembly.Load("Microsoft.SqlServer.Management.SqlStudio.Explorer").GetType("Microsoft.SqlServer.Management.SqlStudio.Explorer.ObjectExplorerService");
            MethodInfo mi = this.GetType().GetMethod("Provider_SelectionChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int nodeCount;
            INodeInformation[] nodes;
            object objectExplorer = GetObjectExplorer();
            // hack to load the OE in R2
            (objectExplorer as IObjectExplorerService).GetSelectedNodes(out nodeCount, out nodes);

            PropertyInfo piContainer = t.GetProperty("Container", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            object objectExplorerContainer = piContainer.GetValue(objectExplorer, null);
            PropertyInfo piContextService = objectExplorerContainer.GetType().GetProperty("Components", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            //object[] indexArgs = { 1 };
            ComponentCollection objectExplorerComponents = piContextService.GetValue(objectExplorerContainer, null) as ComponentCollection;
            object contextService = null;

            foreach (Component component in objectExplorerComponents)
            {
                if (component.GetType().FullName.Contains("ContextService"))
                {
                    contextService = component;
                    break;
                }
            }
            if (contextService == null)
                throw new Exception("Can't find ObjectExplorer ContextService.");

            PropertyInfo piObjectExplorerContext = contextService.GetType().GetProperty("ObjectExplorerContext", System.Reflection.BindingFlags.Public | BindingFlags.Instance);
            object objectExplorerContext = piObjectExplorerContext.GetValue(contextService, null);

            EventInfo ei = objectExplorerContext.GetType().GetEvent("CurrentContextChanged", System.Reflection.BindingFlags.Public | BindingFlags.Instance);

            Delegate del = Delegate.CreateDelegate(ei.EventHandlerType, this, mi);
            ei.AddEventHandler(objectExplorerContext, del);
        }

        public void Init()
        {
            try
            {

                /*if (!Is2008R2)
                    SetNON2008R2ObjectExplorerEventProvider();
                else
                    Set2008R2ObjectExplorerEventProvider();
                 */
                // old way
                //var provider = (IObjectExplorerEventProvider)objectExplorer.GetService(typeof(IObjectExplorerEventProvider));
                //provider.SelectionChanged += new NodesChangedEventHandler(provider_SelectionChanged);
                //ContextService cs = (ContextService)objExplorerService.Container.Components[0];
                //cs.ObjectExplorerContext.CurrentContextChanged += new NodesChangedEventHandler(Provider_SelectionChanged);


            }
            catch (Exception ex)
            {
                // NEED TO LOG
                MyLogger.LogError("Error Initialising object explorer (subscribing selection changed event) " + ex.Message, ex);
            }

            try
            {
              //  System.Threading.Thread.Sleep(80 * 1000);
                var cmdEvents = (EnvDTE.CommandEvents)ServiceCache.ExtensibilityModel.Events.get_CommandEvents("{00000000-0000-0000-0000-000000000000}", 0);
                cmdEvents.AfterExecute += this.AfterExecute;
            }
            catch (Exception ex)
            {

                MyLogger.LogError("Error Initialising object explorer  (subscribing command event)" + ex.Message, ex);
            }
        }

        public event Action<string> OnNewServerConnected;
        public event Action OnServerDisconnected;

        void Provider_SelectionChanged(object sender, NodesChangedEventArgs args)
        {
            try
            {
                var t = args.GetType();
                var pi = t.GetProperty("ChangedNodes");
                var coll = (ICollection)pi.GetValue(args, null);


                foreach (INavigationContext n in coll)
                {
                    if (n!=null && n.Parent == null)
                    {
                        MyLogger.LogMessage("New Server Connected " + n.Name + " -  " + n.Connection.ServerName);
                        // this could mean that new server was added
                        var res = " server " + n.Name + n.Connection.ServerName;
                        if (OnNewServerConnected != null)
                            OnNewServerConnected(n.Connection.ServerName);
                    }
                } 

            }
            catch(Exception ex)
            {
                MyLogger.LogError("Error processing OnSelectionChanged event: " + ex.Message, ex);
            }
       
           

           
        }

        public void AfterExecute(string Guid, int ID, object CustomIn, object CustomOut)
        {
            MyLogger.LogMessage("After execute command:" + ID + " guid:" + Guid);

            // this coul mean that server was removed
            if (ID == 516)
            {
                MyLogger.LogMessage("Server disconnected..!");
                if (OnServerDisconnected != null)
                    OnServerDisconnected();
            }
        }


        private object GetTreeControl_for2008R2Only()
        {
            Type t = GetObjectExplorer().GetType();
            PropertyInfo treeProperty = t.GetProperty("Tree", BindingFlags.Instance | BindingFlags.NonPublic);
            var objectTreeControl = treeProperty.GetValue(GetObjectExplorer(), null);
            return objectTreeControl;
        }

            
       // ugly reflection hack
        private IExplorerHierarchy GetHierarchyForConnection(SqlConnectionInfo connection)
        {
            IExplorerHierarchy hierarchy = null;
            if (!Is2008R2)
            {

                Type t = GetObjectExplorer().GetType();
                var getHierarchyMethod = t.GetMethod("GetHierarchy", BindingFlags.Instance | BindingFlags.NonPublic);

                hierarchy = getHierarchyMethod.Invoke(GetObjectExplorer(), new object[] { connection, string.Empty }) as IExplorerHierarchy;
            }
            else
            {
                var objectTreeControl = GetTreeControl_for2008R2Only();
                var objTreeRype = objectTreeControl.GetType();

                var getHierarchyMethod = objTreeRype.GetMethod("GetHierarchy", BindingFlags.Instance | BindingFlags.Public);

                hierarchy = getHierarchyMethod.Invoke(objectTreeControl, new object[] { connection, string.Empty }) as IExplorerHierarchy;
            }

             // VS2008 here we have additional param string.Empty - need Dependecy Injection in order to make it work?
             return hierarchy;
         
        }

        public IEnumerable<IExplorerHierarchy> GetExplorerHierarchies()
        {
            if(Is2008R2)
            {

                var objectTreeControl = GetTreeControl_for2008R2Only();
                var objTreeRype = objectTreeControl.GetType();

                var hierFieldInfo = objTreeRype.GetField("hierarchies", BindingFlags.Instance | BindingFlags.NonPublic);

                var hierDictionary=(IEnumerable<KeyValuePair<string, IExplorerHierarchy>>)hierFieldInfo.GetValue(objectTreeControl);

                foreach (var keyVaklue in hierDictionary)
                {
                    yield return keyVaklue.Value;
                }
            }
            else
            {
                Type t = GetObjectExplorer().GetType();
                FieldInfo getHierarchyMethod = t.GetField("hierarchies", BindingFlags.Instance | BindingFlags.NonPublic);
                var connHT = (Hashtable)getHierarchyMethod.GetValue(GetObjectExplorer());

                foreach (IExplorerHierarchy srvHerarchy in connHT.Values)
                {
                    yield return srvHerarchy;
                }
            }
        }

        public  List<SqlConnectionInfo> GetAllServers()
        {
            try
            {
                List<SqlConnectionInfo> servers = new List<SqlConnectionInfo>();

                foreach (IExplorerHierarchy srvHerarchy in GetExplorerHierarchies())
                {
                    IServiceProvider provider = srvHerarchy.Root as IServiceProvider;
                    if (provider != null)
                    {
                        INodeInformation containedItem = provider.GetService(typeof(INodeInformation)) as INodeInformation;
                        servers.Add(containedItem.Connection as SqlConnectionInfo);
                    }
                }

                return servers;
            }
            catch (Exception ex)
            {
                MyLogger.LogError("ObjectExplorer manager failed:" + ex.Message,ex);
                throw;
            }
          
        }


        // select server on object window
        public void SelectServer(SqlConnectionInfo connection)
        {

            IExplorerHierarchy hierarchy = GetHierarchyForConnection(connection);
            SelectNode(hierarchy.Root);
        }

        private MethodInfo _editTableMethod = null;
        public void OpenTable2(NamedSmoObject tbl, SqlConnectionInfo connection,Server server)
        {
             string fileName = null;
            // step1 - get script to edit table - SelectFromTableOrView(Server server, Urn urn, int topNValue)

            // step2 - create a file

            
             try
             {
                 var t =
                     Type.GetType(
                         "Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.OpenTableHelperClass,ObjectExplorer",
                         true, true);
                 var miSelectFromTable = t.GetMethod("SelectFromTableOrView", BindingFlags.Static | BindingFlags.Public);


                 //signature is: public static string SelectFromTableOrView(Server server, Urn urn, int topNValue)
                 string script = (string) miSelectFromTable.Invoke(null, new object[] {server, tbl.Urn, 200});

                 fileName = CreateFile(script);



                 // invoke designer
                 var mc = new ManagementStudioController.ManagedConn();
                 mc.Connection = connection;
                 if (_editTableMethod == null)
                 {

                     foreach (var mi in
                         ServiceCache.ScriptFactory.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                         )
                     {
                         if (mi.Name == "CreateDesigner" && mi.GetParameters().Length == 5)
                         {
                             _editTableMethod = mi;
                             break;
                         }
                     }

                 }

                 if (_editTableMethod != null)
                 {

                     _editTableMethod.Invoke(ServiceCache.ScriptFactory, 
                            new object[] {DocumentType.OpenTable,DocumentOptions.ManageConnection, new Urn(tbl.Urn.ToString() + "/Data"), mc,fileName});
                 }
                 else
                 {
                     MyLogger.LogError("Could not find CreateDesigner method");
                 }

               

             }
             catch(Exception ex)
             {
                 MyLogger.LogError("Failed OpenTable2",ex);
             }
             finally
             {
                 if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                 {
                     File.Delete(fileName);
                 }
             }

         

        }


        public static string CreateFile(string script)
        {
            string path = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", new object[] { Path.GetTempFileName(), "dtq" });
            StringBuilder builder = new StringBuilder();
            builder.Append("[D035CF15-9EDB-4855-AF42-88E6F6E66540, 2.00]\r\n");
            builder.Append("Begin Query = \"Query1.dtq\"\r\n");
            builder.Append("Begin RunProperties =\r\n");
            builder.AppendFormat("{0}{1}{2}", "SQL = \"", script, "\"\r\n");
            builder.Append("ParamPrefix = \"@\"\r\n");
            builder.Append("ParamSuffix = \"\"\r\n");
            builder.Append("ParamSuffix = \"\\\"\r\n");
            builder.Append("End\r\n");
            builder.Append("End\r\n");
            StreamWriter writer = new StreamWriter(path, false, Encoding.Unicode);
            writer.Write(builder.ToString());
            writer.Close();
            return path;
        }




        internal void OpenTable(NamedSmoObject objectToSelect, SqlConnectionInfo connection)
        {
            try
            {
                 IExplorerHierarchy hierarchy = GetHierarchyForConnection(connection);
                if (hierarchy == null)
                {
                    return; // there is nothing we can really do if we don't have one of these
                }
                HierarchyTreeNode databasesNode = GetUserDatabasesNode(hierarchy.Root);

                var resultNode = GetNode(databasesNode, objectToSelect);

                //MSSQLController.Current.SearchWindow.Activate();

                if (resultNode != null)
                    OpenTable(resultNode, connection);

                 
            }
            catch(Exception ex)
            {
                MyLogger.LogError("Error opening table: " + objectToSelect.Name ,ex);
            }

        
        }


        internal void SelectSMOObjectInObjectExplorer(NamedSmoObject objectToSelect, SqlConnectionInfo connection)
        {
            if (objectToSelect.State == SqlSmoState.Dropped)
            {
                MyLogger.LogMessage("Trying to locate dropped object:" + objectToSelect.Name);
                return;
            }
            IExplorerHierarchy hierarchy = GetHierarchyForConnection(connection);
            if (hierarchy == null)
            {
                return; // there is nothing we can really do if we don't have one of these
            }
            HierarchyTreeNode databasesNode = GetUserDatabasesNode(hierarchy.Root);
            var resultNode = SelectSMOObject(databasesNode, objectToSelect);
            if (resultNode != null)
                SelectNode(resultNode);
        }

        private HierarchyTreeNode GetUserDatabasesNode(HierarchyTreeNode rootNode)
        {
            if (rootNode != null)
            {
                // root should always be expandable
                if (rootNode.Expandable)
                {
                    EnumerateChildrenSynchronously(rootNode);
                    rootNode.Expand();

                    // TODO this is horrible code - it assumes the first node will ALWAYS be the "Databases" node in the object explorer, which may not always be the case
                    // however I couldn't think of a clean way to always find the right node
                    return (HierarchyTreeNode)rootNode.Nodes[0];
                }
            }
            return null;
        }

        private string BuildMatchingPathExpressionForDepth(NamedSmoObject objectToSelect, string parentNodePath, int level, out bool atFinalLevel)
        {
            atFinalLevel = false;
            string expression = string.Empty;

            // Databases node is at level 1
            switch (level)
            {
                case 2: // database level
                    Regex re = new Regex(".*Database\\[@Name='(.*?)']?");
                    Match m = re.Match(objectToSelect.Urn);
                    expression = parentNodePath + "\\" + m.Groups[1].Captures[0];
                    atFinalLevel = (objectToSelect is Database);
                    break;

                case 3:
                    if (objectToSelect is StoredProcedure || objectToSelect is UserDefinedFunction)
                    {
                        expression = parentNodePath + "\\Programmability";
                    }
                    else if (objectToSelect is Table)
                    {
                        expression = parentNodePath + "\\Tables";
                    }
                    else if (objectToSelect is Microsoft.SqlServer.Management.Smo.View)
                    {
                        expression = parentNodePath + "\\Views";
                    }
                    break;

                case 4:
                    if (objectToSelect is StoredProcedure)
                    {
                        expression = parentNodePath + "\\Stored Procedures";
                    }
                    else if (objectToSelect is UserDefinedFunction)
                    {
                        expression = parentNodePath + "\\Functions";
                    }
                    else
                    {
                        expression = parentNodePath + "\\" + GetSchemaQualifiedNameForSmoObject(objectToSelect);
                        atFinalLevel = true;
                    }
                    break;

                case 5:
                    if (objectToSelect is UserDefinedFunction)
                    {
                        switch (((UserDefinedFunction)objectToSelect).FunctionType)
                        {
                            case UserDefinedFunctionType.Scalar:
                                expression = parentNodePath + "\\Scalar-valued Functions";
                                break;
                            case UserDefinedFunctionType.Table:
                                expression = parentNodePath + "\\Table-valued Functions";
                                break;
                        }
                    }
                    else
                    {

                        expression = parentNodePath + "\\" + GetSchemaQualifiedNameForSmoObject(objectToSelect);
                        atFinalLevel = true;
                    }
                    break;
                case 6:
                    expression = parentNodePath + "\\" + GetSchemaQualifiedNameForSmoObject(objectToSelect);
                    atFinalLevel = true;
                    break;
            }

            return expression;
        }

        private string GetSchemaQualifiedNameForSmoObject(NamedSmoObject namedObject)
        {
            Regex re = new Regex(".*\\[@Name='(.*?)' and @Schema='(.*?)']");
            Match m = re.Match(namedObject.Urn);
            string schemaQualifiedName = m.Groups[2].Captures[0] + "." + m.Groups[1].Captures[0];
            return schemaQualifiedName; // Named SMO object has a FullQualifiedName property but it is internal
        }

        private HierarchyTreeNode GetNode(HierarchyTreeNode node, NamedSmoObject objectToSelect)
        {
            if (node != null)
            {
                //EnumerateChildrenSynchronously(node);
                bool atFinalLevel;
                string pattern = BuildMatchingPathExpressionForDepth(objectToSelect, node.FullPath, node.Level + 1, out atFinalLevel);

                foreach (HierarchyTreeNode child in node.Nodes)
                {
                    if (string.Compare(child.FullPath, pattern, true) == 0)
                    {
                        if (atFinalLevel)
                        {
                            return child;// SelectNode(child);
                        }
                        else
                        {
                            return GetNode(child, objectToSelect);
                        }
                    }
                }
            }
            return null;
        }

        private HierarchyTreeNode SelectSMOObject(HierarchyTreeNode node, NamedSmoObject objectToSelect)
        {
            if (node != null)
            {
                if (node.Expandable)
                {
                    EnumerateChildrenSynchronously(node);
                    node.Expand();

                    bool atFinalLevel;
                    string pattern = BuildMatchingPathExpressionForDepth(objectToSelect, node.FullPath, node.Level + 1, out atFinalLevel);

  

                    foreach (HierarchyTreeNode child in node.Nodes)
                    {
                        if (string.Compare(child.FullPath, pattern, true) == 0)
                        {
                            if (atFinalLevel)
                            {
                                return child;// SelectNode(child);
                            }
                            else
                            {
                                return SelectSMOObject(child, objectToSelect);
                            }
                        }
                    }

                }
            }

              return null;
        }

        private void OpenTable(HierarchyTreeNode node,SqlConnectionInfo connection)
        {

            var t = Type.GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.OpenTableHelperClass,ObjectExplorer", true, true);
            var mi = t.GetMethod("EditTopNRows", BindingFlags.Static | BindingFlags.Public);


            var ncT = Type.GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.NodeContext,ObjectExplorer",true,true);
            IServiceProvider provider = node as IServiceProvider;
            INodeInformation containedItem = provider.GetService(typeof(INodeInformation)) as INodeInformation;
         
            var inst = Activator.CreateInstance(ncT, containedItem);
            if (inst == null)
                throw new Exception("Cannot create type" + ncT.ToString());
            mi.Invoke(null, new object[] { containedItem, 200 });
         
        }

        private void SelectNode(HierarchyTreeNode node)
        {
            IServiceProvider provider = node as IServiceProvider;
            if (provider != null)
            {
                INodeInformation containedItem = provider.GetService(typeof(INodeInformation)) as INodeInformation;
                if (containedItem != null)
                {
                    IObjectExplorerService objExplorer =GetObjectExplorer();


                    objExplorer.SynchronizeTree(containedItem);
                }
            }
        }

        // another exciting opportunity to use reflection
        private void EnumerateChildrenSynchronously(HierarchyTreeNode node)
        {
            Type t = node.GetType();
            MethodInfo method = t.GetMethod("EnumerateChildren", new Type[] { typeof(Boolean) });
            if (method != null)
            {
                method.Invoke(node, new object[] { false });
            }
            else
            {
                // fail
                node.EnumerateChildren();
            }
        }
    }
}