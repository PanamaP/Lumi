using System.Runtime.CompilerServices;
using Lumi;
using Lumi.Core;
using Lumi.Core.Animation;

namespace TodoApp;

public class MainWindow : Window
{
    private readonly List<TodoItem> _todos = new();
    private string _filter = "all"; // "all", "active", "done"

    // Cached element references
    private InputElement? _input;
    private Element? _todoList;
    private TextElement? _taskCount;
    private TextElement? _remainingText;

    public MainWindow()
    {
        Title = "Lumi — Todo App";
        Width = 700;
        Height = 600;

        var outputDir = AppContext.BaseDirectory;
        var sourceDir = GetSourceDirectory();

        LoadTemplate(Path.Combine(outputDir, "MainWindow.html"));
        LoadStyleSheet(Path.Combine(outputDir, "MainWindow.css"));

        HtmlPath = Path.Combine(sourceDir, "MainWindow.html");
        CssPath = Path.Combine(sourceDir, "MainWindow.css");
        EnableHotReload = true;
    }

    public override void OnReady()
    {
        _input = FindById("todo-input") as InputElement;
        _todoList = FindById("todo-list");
        _taskCount = FindById("task-count") as TextElement;
        _remainingText = FindById("remaining-text") as TextElement;

        // Add todo on button click
        FindById("btn-add")?.On("Click", (_, _) => AddTodo());

        // Add todo on Enter key
        if (_input != null)
        {
            _input.On("KeyDown", (_, e) =>
            {
                if (e is RoutedKeyEvent ke && ke.Key == KeyCode.Enter)
                    AddTodo();
            });
        }

        // Filter buttons
        FindById("filter-all")?.On("Click", (_, _) => SetFilter("all"));
        FindById("filter-active")?.On("Click", (_, _) => SetFilter("active"));
        FindById("filter-done")?.On("Click", (_, _) => SetFilter("done"));

        // Clear completed
        FindById("btn-clear")?.On("Click", (_, _) => ClearCompleted());

        RenderList();
    }

    private void AddTodo()
    {
        if (_input == null) return;
        var text = _input.Value.Trim();
        if (string.IsNullOrEmpty(text)) return;

        _todos.Add(new TodoItem { Text = text });
        _input.Value = "";
        _input.MarkDirty();

        RenderList();
    }

    private void ToggleTodo(int index)
    {
        if (index < 0 || index >= _todos.Count) return;
        _todos[index].IsDone = !_todos[index].IsDone;
        RenderList();
    }

    private void DeleteTodo(int index)
    {
        if (index < 0 || index >= _todos.Count) return;
        _todos.RemoveAt(index);
        RenderList();
    }

    private void ClearCompleted()
    {
        _todos.RemoveAll(t => t.IsDone);
        RenderList();
    }

    private void SetFilter(string filter)
    {
        _filter = filter;

        // Update active filter button styling
        var filterIds = new[] { "filter-all", "filter-active", "filter-done" };
        var filterNames = new[] { "all", "active", "done" };
        for (int i = 0; i < filterIds.Length; i++)
        {
            var btn = FindById(filterIds[i]);
            if (btn == null) continue;
            if (filterNames[i] == _filter)
                btn.Classes.Add("active");
            else
                btn.Classes.Remove("active");
            btn.MarkDirty();
        }

        RenderList();
    }

    private void RenderList()
    {
        if (_todoList == null) return;

        _todoList.ClearChildren();

        var filtered = _filter switch
        {
            "active" => _todos.Where(t => !t.IsDone).ToList(),
            "done" => _todos.Where(t => t.IsDone).ToList(),
            _ => _todos.ToList()
        };

        if (filtered.Count == 0)
        {
            var empty = new BoxElement("div");
            empty.Classes.Add("empty-state");

            var icon = new TextElement("📋");
            icon.Classes.Add("empty-icon");
            empty.AddChild(icon);

            var msg = new TextElement(_filter == "done" ? "No completed tasks" :
                                     _filter == "active" ? "All tasks completed!" :
                                     "Add your first task above");
            msg.Classes.Add("empty-text");
            empty.AddChild(msg);

            _todoList.AddChild(empty);
        }
        else
        {
            foreach (var todo in filtered)
            {
                int todoIndex = _todos.IndexOf(todo);
                var row = CreateTodoRow(todo, todoIndex);
                _todoList.AddChild(row);

                // Fade in animation
                row.ComputedStyle.Opacity = 0;
                row.Animate()
                    .Property("opacity", 0, 1)
                    .Duration(0.25f)
                    .Easing(Easing.EaseOutCubic)
                    .Start();
            }
        }

        UpdateCounts();
    }

    private Element CreateTodoRow(TodoItem todo, int index)
    {
        var row = new BoxElement("div");
        row.Classes.Add("todo-item");

        // Checkbox
        var checkbox = new BoxElement("div");
        checkbox.Classes.Add("todo-checkbox");
        if (todo.IsDone)
        {
            checkbox.Classes.Add("checked");
            var mark = new TextElement("✓");
            mark.Classes.Add("todo-checkbox-mark");
            checkbox.AddChild(mark);
        }

        int capturedIndex = index;
        checkbox.On("Click", (_, _) => ToggleTodo(capturedIndex));
        row.AddChild(checkbox);

        // Text
        var text = new TextElement(todo.Text);
        text.Classes.Add("todo-text");
        if (todo.IsDone)
            text.Classes.Add("completed");
        row.AddChild(text);

        // Delete button
        var deleteBtn = new BoxElement("button");
        deleteBtn.Classes.Add("btn");
        deleteBtn.Classes.Add("btn-delete");
        var deleteText = new TextElement("Delete");
        deleteBtn.AddChild(deleteText);
        deleteBtn.On("Click", (_, _) => DeleteTodo(capturedIndex));
        row.AddChild(deleteBtn);

        return row;
    }

    private void UpdateCounts()
    {
        int total = _todos.Count;
        int remaining = _todos.Count(t => !t.IsDone);

        if (_taskCount != null)
        {
            _taskCount.Text = $"{total} task{(total != 1 ? "s" : "")}";
            _taskCount.MarkDirty();
        }

        if (_remainingText != null)
        {
            _remainingText.Text = $"{remaining} item{(remaining != 1 ? "s" : "")} left";
            _remainingText.MarkDirty();
        }
    }

    private static string GetSourceDirectory([CallerFilePath] string callerPath = "")
        => Path.GetDirectoryName(callerPath)!;

    private class TodoItem
    {
        public string Text { get; set; } = "";
        public bool IsDone { get; set; }
    }
}
