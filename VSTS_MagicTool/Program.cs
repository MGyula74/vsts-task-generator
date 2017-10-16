using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.IO;
using System.Text;

namespace VSTS_MagicTool
{
    class Program
    {
        static string[,] taskNameConv = { 
            { "Design", "1", "" }, 
            { "Development", "2", "" }, 
            { "Testing", "3", "Test" },
            { "Deployment", "4", "Staging" }, 
            { "Testing", "6", "Staging" }, 
            { "Deployment", "8", "Live" }
        };

        static string personalAccessToken = "";
        static Uri accountUri;
        static string projectStr = "";
        static bool create = false;

        static void Main(string[] args)
        {
            loadConfiguration();

            if (args.Length <= 2)
            {
                //Uri accountUri = new Uri(args[0]);     // Account URL, for example: https://fabrikam.visualstudio.com                
                //String personalAccessToken = args[1];  // See https://www.visualstudio.com/docs/integrate/get-started/authentication/pats                
                int workItemId = int.Parse(args[0]);   // ID of a work item, for example: 12

                if (args.Length == 2 && args[1] == "create")
                    create = true;

                // Create a connection to the account
                VssConnection connection = new VssConnection(accountUri, new VssBasicCredential(string.Empty, personalAccessToken));

                // Get an instance of the work item tracking client
                WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

                try
                {
                    // Get the specified work item
                    WorkItem workitem = witClient.GetWorkItemAsync(workItemId).Result;

                    //// Output the work item's field values
                    //foreach (var field in workitem.Fields)
                    //{
                    //    Console.WriteLine("  {0}: {1}", field.Key, field.Value);
                    //}

                    //if (workitem.Relations != null)
                    //{
                    //    foreach (WorkItemRelation wrel in workitem.Relations)
                    //    {
                    //        Console.WriteLine("{0} - {1}", wrel.Title, wrel.Url);
                    //    }
                    //}

                    //foreach (KeyValuePair<string, object> obj in workitem.Links.Links)
                    //{
                    //    ReferenceLink rlink = (ReferenceLink)obj.Value;
                    //    Console.WriteLine("{0} - {1}", obj.Key, rlink.Href);
                    //}
                    //Console.WriteLine("-------------------------------------------------------");

                    GetWorkItemFullyExpanded(connection, workItemId);
                    Console.WriteLine("  WorkItem.URL = {0}", workitem.Url);

                    //CreateAndLinkToWorkItem(connection, workItemId, workitem.Url);

                    /*WorkItem wi1 = CreateTask(connection, Convert.ToString(workitem.Fields["System.Title"]), workitem.Url, Convert.ToString(workitem.Fields["Microsoft.VSTS.Common.Priority"]), 
                        taskNameConv[1], DateTime.Now, false, "", "", "");

                    WorkItem wi2 = CreateTask(connection, Convert.ToString(workitem.Fields["System.Title"]), workitem.Url, Convert.ToString(workitem.Fields["Microsoft.VSTS.Common.Priority"]),
                        taskNameConv[2], DateTime.Now, false, "", wi1.Url, "");*/

                    if (create)
                    {
                        Console.WriteLine("-------------------------------------------------------");
                        Console.WriteLine("Creating child task...");

                        WorkItem prevItem = null;
                        for (int ix = 0; ix < (taskNameConv.Length / 3); ix++)
                        {
                            string titleStr = Convert.ToString(workitem.Fields["System.Title"]);
                            titleStr = String.Format("{0} - {1} {2}", taskNameConv[ix, 1], workItemId, titleStr);

                            WorkItem wi = CreateTask(connection, titleStr, workitem.Url,
                                Convert.ToString(workitem.Fields["Microsoft.VSTS.Common.Priority"]),
                                taskNameConv[ix, 0], DateTime.Now, taskNameConv[ix, 1] == "6", "", (prevItem != null) ? prevItem.Url : null, taskNameConv[ix, 2]);

                            prevItem = wi;
                        }
                    }
                }
                catch (AggregateException aex)
                {
                    VssServiceException vssex = aex.InnerException as VssServiceException;
                    if (vssex != null)
                    {
                        Console.WriteLine(vssex.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Usage: ConsoleApp {accountUri} {personalAccessToken} {workItemId}");
            }
        }

        public static WorkItem GetWorkItemFullyExpanded(VssConnection connection, int id)
        {
            //VssConnection connection = Context.Connection;
            WorkItemTrackingHttpClient workItemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();

            WorkItem workitem = workItemTrackingClient.GetWorkItemAsync(id, expand: WorkItemExpand.All).Result;

            Console.WriteLine("Fields: ");
            foreach (var field in workitem.Fields)
            {
                Console.WriteLine("  {0}: {1}", field.Key, field.Value);
            }

            if (workitem.Relations != null)
            {
                Console.WriteLine("Relations: ");
                foreach (var relation in workitem.Relations)
                {
                    Console.WriteLine("  {0} {1}", relation.Rel, relation.Url);
                }
            }
            return workitem;
        }

        public static WorkItem CreateTask(VssConnection connection, string title, string parentLinkUrl, string priority, string activity, DateTime? dueDate, 
                                     bool external = false, string estimate = "", string predecessorLinkUrl = "", string environment = "")
        {
            //public static WorkItem CreateAndLinkToWorkItem(VssConnection connection, string linkUrl)
            //{
            //    string title = "My new work item with links";
            //string description = ""; // "This is a new work item that has a link also created on it.";
            //string linkUrl = "https://integrate.visualstudio.com";

            JsonPatchDocument patchDocument = new JsonPatchDocument();

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = title
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/BUNZLAgile.B_Priority",
                    Value = priority
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/BUNZLAgile.B_EnvironmentInfo",
                    Value = environment
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Common.Activity",
                    Value = activity
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/BUNZLAgile.B_TaskDomain",
                    Value = external ? "External" : "Internal"
                }
            );

            /*patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Scheduling.DueDate",
                    Value = dueDate
                }
            );*/

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork",
                    Value = estimate
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate",
                    Value = estimate
                }
            );

            //patchDocument.Add(
            //    new JsonPatchOperation()
            //    {
            //        Operation = Operation.Add,
            //        Path = "/fields/System.Description",
            //        Value = description
            //    }
            //);

            //patchDocument.Add(
            //    new JsonPatchOperation()
            //    {
            //        Operation = Operation.Add,
            //        Path = "/fields/System.History",
            //        Value = "Jim has the most context around this."
            //    }
            //);

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = parentLinkUrl,
                        //attributes = new
                        //{
                        //    comment = "decomposition of work"
                        //}
                    }
                }
            );

            if (!String.IsNullOrEmpty(predecessorLinkUrl))
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "System.LinkTypes.Dependency-Reverse",
                        url = predecessorLinkUrl
                    }
                }
            );

            //VssConnection connection = Context.Connection;
            WorkItemTrackingHttpClient workItemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();

            // Get the project to create the sample work item in
            //TeamProjectReference project = ClientSampleHelpers.FindAnyProject(this.Context);

            WorkItem result = workItemTrackingClient.CreateWorkItemAsync(patchDocument, projectStr, "Task").Result;

            Console.WriteLine("Result workItem.Id = {0}", result.Id);

            return result;
        }

        static void loadConfiguration()
        {
            string text = File.ReadAllText(@"magic.cfg", Encoding.UTF8);
            text = text.Replace("\r", "");
            string[] confLines = text.Split('\n');

            for (int ix = 0; ix < confLines.Length; ix++)
            {
                string[] keyVal = confLines[ix].Split('=');
                switch (keyVal[0])
                {
                    case "TOKEN":
                        personalAccessToken = keyVal[1];
                        break;
                    case "URL":
                        accountUri = new Uri(keyVal[1]);
                        break;
                    case "PROJECT":
                        projectStr = keyVal[1];
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
