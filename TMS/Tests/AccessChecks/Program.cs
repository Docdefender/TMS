using TMS.Models;
using TMS.Services;

var count = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    count++;
    Console.WriteLine("PASS: " + name);
}
var admin = new AccessActor("admin", true, false, false, []);
var manager = new AccessActor("manager", false, true, false, [1, 2]);
var outsider = new AccessActor("outside", false, true, false, [3]);
var member = new AccessActor("member", false, false, true, []);
var project = new Project { Id = 1, DepartmentId = 2, ManagerUserId = "another-manager" };
var rights = AccessRules.Project(manager, project);
Check(rights.View && rights.Edit && rights.Delete && rights.CreateTask && !rights.ChangeStatus, "Department Manager can manage, but nonparticipant cannot change project status");
Check(!AccessRules.Project(outsider, project).View, "Unrelated department is hidden");
project.Members.Add(new ProjectMember { UserId = outsider.UserId });
rights = AccessRules.Project(outsider, project);
Check(rights.View && !rights.Edit && !rights.Delete && rights.CreateTask && rights.ChangeStatus, "External project Manager has participation rights only");
project.Members.Add(new ProjectMember { UserId = member.UserId });
rights = AccessRules.Project(member, project);
Check(rights.View && rights.CreateTask && !rights.Edit && !rights.Delete && !rights.ChangeStatus, "Member can create tasks in joined project only");
Check(AccessRules.Project(admin, project) == new ProjectAccess(true, true, true, true, true), "Admin project override");
var task = new TaskItem { Id = 1, ProjectId = 1, Project = project, CreatedByUserId = "creator", AssignedToUserId = outsider.UserId, FirstAssignedByUserId = "creator" };
var taskRights = AccessRules.Task(outsider, task, false);
Check(taskRights.View && taskRights.Edit && taskRights.Contribute && !taskRights.Delete, "External assignee Manager can edit but cannot delete");
task.AssignedToUserId = "next";
task.HistoryAccess.Add(new TaskHistoryAccess { UserId = outsider.UserId });
taskRights = AccessRules.Task(outsider, task, false);
Check(taskRights == new TaskAccess(true, false, false, false), "Transferred external task is read only");
task.HistoryAccess.First().RevokedAt = DateTime.UtcNow;
Check(!AccessRules.Task(outsider, task, false).View, "Admin revocation removes history-only access");
Check(AccessRules.Task(outsider, task, true).View, "Revocation does not remove independent project access");
task.CreatedByUserId = outsider.UserId;
taskRights = AccessRules.Task(outsider, task, false);
Check(taskRights.Edit && taskRights.Delete && taskRights.View, "Manager creator retains management after delegation");
task.CreatedByUserId = member.UserId;
Check(AccessRules.Task(member, task, false) == new TaskAccess(true, false, false, false), "Member creator retains reading only after assignment to another");
task.AssignedToUserId = member.UserId;
Check(AccessRules.Task(member, task, false) == new TaskAccess(true, false, false, true), "Assigned Member can contribute but not edit or delete");
Check(AccessRules.Task(manager, task, false) == new TaskAccess(true, true, true, true), "Department management implies task visibility");
Check(AccessRules.Task(admin, task, false) == new TaskAccess(true, true, true, true), "Admin task override");
task.IsDeleted = true;
Check(!AccessRules.Task(admin, task, true).View, "Deleted task cannot be accessed through active resource path");
task.IsDeleted = false; project.IsDeleted = true;
Check(!AccessRules.Task(member, task, true).View, "Deleted parent hides assigned task");
Console.WriteLine($"{count} access scenarios passed.");

if (args.Contains("--sql")) await SqlChecks.RunAsync();
