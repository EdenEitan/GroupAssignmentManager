import { useCallback, useEffect, useMemo, useState } from 'react';
import { ApiError, projectsApi, tasksApi, usersApi } from './api';
import type { Priority, Project, Status, TaskItem, User } from './types';

const statuses: Status[] = ['Open', 'In Progress', 'Completed'];
const dateInput = (iso: string | null) => iso ? iso.slice(0, 10) : '';
const dateLabel = (iso: string | null) => iso ? new Date(iso).toLocaleDateString('en-GB') : 'No deadline';

export default function App() {
  const [users, setUsers] = useState<User[]>([]);
  const [currentId, setCurrentId] = useState(localStorage.getItem('demoUser') || '');
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedId, setSelectedId] = useState('');
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const [filter, setFilter] = useState('All');
  const [editing, setEditing] = useState<TaskItem | null>(null);
  const [showTaskForm, setShowTaskForm] = useState(false);
  const current = users.find(x => x.userId === currentId);
  const selected = projects.find(x => x.projectId === selectedId);
  const members = users.filter(x => selected?.memberIds.includes(x.userId));
  const name = (id: string | null) => users.find(x => x.userId === id)?.name || 'Unassigned';

  const refreshUsers = useCallback(async () => setUsers(await usersApi.list()), []);
  const refreshProjects = useCallback(async (id: string) => setProjects(id ? await projectsApi.list(id) : []), []);
  const refreshTasks = useCallback(async (id: string) => setTasks(id ? await tasksApi.list(id) : []), []);
  useEffect(() => { refreshUsers().catch(showError); }, [refreshUsers]);
  useEffect(() => {
    localStorage.setItem('demoUser', currentId);
    setSelectedId('');
    refreshProjects(currentId).catch(showError);
  }, [currentId, refreshProjects]);
  useEffect(() => { refreshTasks(selectedId).catch(showError); }, [selectedId, refreshTasks]);
  function showError(e: unknown) {
    setError(e instanceof Error ? e.message : 'Something went wrong.');
    setNotice('');
  }
  async function run(action: () => Promise<unknown>, message: string, reload = true) {
    setError(''); setNotice(''); setBusy(true);
    try {
      await action();
      if (reload) { await refreshUsers(); await refreshProjects(currentId); await refreshTasks(selectedId); }
      setNotice(message);
      return true;
    } catch (e) {
      if (e instanceof ApiError && e.status === 409 && selectedId) {
        await refreshTasks(selectedId).catch(() => {});
        setEditing(null);
        showError(new Error('Someone changed this task first. The board was refreshed; check the new version and try again.'));
      } else showError(e);
      return false;
    } finally { setBusy(false); }
  }
  async function createUser(form: FormData) {
    await run(async () => {
      const user = await usersApi.create(String(form.get('name')), String(form.get('email')));
      setCurrentId(user.userId);
    }, 'Member created.');
  }
  async function createProject(form: FormData) {
    await run(async () => {
      const project = await projectsApi.create(String(form.get('projectName')), String(form.get('courseName')),
        String(form.get('description')), String(form.get('deadline')), currentId);
      await refreshProjects(currentId);
      setSelectedId(project.projectId);
    }, 'Project created.');
  }
  async function saveTask(form: FormData) {
    const title = String(form.get('title'));
    const description = String(form.get('description'));
    const deadline = String(form.get('deadline'));
    const priority = String(form.get('priority')) as Priority;
    const ok = await run(() => editing
      ? tasksApi.details(editing, { title, description, deadline, priority })
      : tasksApi.create(selectedId, title, description, deadline, priority), editing ? 'Task updated.' : 'Task created.');
    if (ok) { setShowTaskForm(false); setEditing(null); }
  }
  const visible = useMemo(() => tasks.filter(task => filter === 'All' ||
    (filter === 'Mine' && task.assigneeId === currentId) ||
    (filter === 'Unassigned' && !task.assigneeId) ||
    (filter === 'Needs help' && task.needsHelp) || task.status === filter), [tasks, filter, currentId]);
  const completed = tasks.filter(x => x.status === 'Completed').length;

  return <div className="shell">
    <header className="topbar">
      <div className="brand"><span className="brand-icon">◈</span><div><strong>GROUPSPACE</strong><small>Assignment manager</small></div></div>
      <div className="identity"><span className="online-dot" /> Working as
        <select aria-label="Choose demo user" value={currentId} onChange={e => setCurrentId(e.target.value)}>
          <option value="">Choose a member</option>
          {users.map(user => <option key={user.userId} value={user.userId}>{user.name}</option>)}
        </select>
      </div>
    </header>
    <main>
      <div className="intro"><div><p className="eyebrow">YOUR WORKSPACE</p><h1>Make group work <em>work.</em></h1>
        <p>Plan projects, share tasks and keep everyone on the same page.</p></div>
        <button className="quiet" onClick={() => { refreshUsers().catch(showError); refreshProjects(currentId).catch(showError); refreshTasks(selectedId).catch(showError); setNotice('Latest data loaded.'); }} disabled={busy}>↻ Refresh board</button>
      </div>
      {error && <div className="alert error" role="alert">{error}<button onClick={() => setError('')}>×</button></div>}
      {notice && <div className="alert success">{notice}<button onClick={() => setNotice('')}>×</button></div>}
      {!current && <div className="welcome panel"><h2>First, choose who you are</h2>
        <p>Pick a member above, or create one to start a project. User selection is for this demo; it is not authentication.</p>
        <form className="inline-form" onSubmit={e => { e.preventDefault(); const form = new FormData(e.currentTarget); void createUser(form); }}>
          <input name="name" placeholder="Your name" required /><input name="email" placeholder="Email address" type="email" required />
          <button disabled={busy}>Create member</button>
        </form></div>}
      {current && <div className="layout">
        <aside>
          <div className="section-heading"><h2>Projects</h2><span>{projects.length}</span></div>
          <div className="project-list">{projects.map(project => <button key={project.projectId}
            className={'project-link ' + (selectedId === project.projectId ? 'active' : '')}
            onClick={() => { setSelectedId(project.projectId); setFilter('All'); }}>
            <span className="project-mark">▦</span><span><strong>{project.projectName}</strong><small>{project.courseName}</small></span></button>)}
            {!projects.length && <p className="muted">No projects yet. Create the first one below.</p>}</div>
          <form className="panel side-form" onSubmit={e => { e.preventDefault(); const el = e.currentTarget; void createProject(new FormData(el)).then(() => el.reset()); }}>
            <h3>+ New project</h3><label>Project name<input name="projectName" placeholder="Group assignment" required /></label>
            <label>Course<input name="courseName" placeholder="Software Engineering" required /></label>
            <label>Description<textarea name="description" rows={2} placeholder="What are you building?" /></label>
            <label>Deadline<input name="deadline" type="date" /></label><button disabled={busy}>Create project</button>
          </form>
          <div className="side-footer">DEMO MODE · MONGODB PERSISTENCE</div>
        </aside>
        <section className="content">
          {!selected ? <div className="empty panel"><div className="empty-icon">▦</div><h2>Select a project</h2>
            <p>Choose a project on the left, or create a new one to organize your team's work.</p></div> :
          <>
            <div className="project-header"><div><p className="eyebrow">{selected.courseName}</p><h2>{selected.projectName}</h2>
              <p>{selected.description || 'A shared space for your team.'} · Due {dateLabel(selected.deadline)}</p></div>
              <button onClick={() => { setEditing(null); setShowTaskForm(true); }} disabled={busy}>+ New task</button></div>
            <div className="stats">
              <div><strong>{tasks.length}</strong><span>Total tasks</span></div>
              <div><strong>{tasks.filter(x => x.status === 'In Progress').length}</strong><span>In progress</span></div>
              <div><strong>{completed}</strong><span>Completed</span></div>
              <div><strong>{tasks.filter(x => x.needsHelp).length}</strong><span>Need help</span></div>
            </div>
            <div className="members"><div><strong>Project team</strong><p>{members.map(x => x.name).join(', ')}</p></div>
              <select aria-label="Add a member" value="" disabled={busy || users.every(x => selected.memberIds.includes(x.userId))}
                onChange={e => { if (e.target.value) void run(() => projectsApi.addMember(selectedId, e.target.value), 'Member added.'); }}>
                <option value="">+ Add member</option>{users.filter(x => !selected.memberIds.includes(x.userId)).map(x =>
                  <option key={x.userId} value={x.userId}>{x.name}</option>)}</select></div>
            <div className="board-heading"><div><h3>Task board</h3><p>{visible.length} shown · {tasks.length ? Math.round(completed / tasks.length * 100) : 0}% complete</p></div>
              <div className="filters">{['All', 'Mine', 'Unassigned', 'Needs help'].map(item =>
                <button key={item} className={filter === item ? 'chosen' : ''} onClick={() => setFilter(item)}>{item}</button>)}</div></div>
            <div className="board">{statuses.map(status => <div className="column" key={status}>
              <div className="column-title"><span className={'status-dot ' + status.replace(' ', '')} />{status}
                <span className="count">{visible.filter(x => x.status === status).length}</span></div>
              {visible.filter(x => x.status === status).map(task => <article className="task-card" key={task.id}>
                <div className="card-top"><span className={'priority ' + task.priority}>{task.priority}</span><span className="version">v{task.version}</span></div>
                <h4>{task.title}</h4><p className="task-description">{task.description || 'No description yet.'}</p>
                {task.needsHelp && <span className="help-label">✳ Needs help</span>}
                <div className="task-meta"><span>◷ {dateLabel(task.deadline)}</span><span>◉ {name(task.assigneeId)}</span></div>
                {!!task.assignmentRequests.length && <p className="requests">Requests: {task.assignmentRequests.map(name).join(', ')}</p>}
                <div className="task-actions">
                  {!task.assigneeId && task.status !== 'Completed' && !task.assignmentRequests.includes(currentId) &&
                    <button disabled={busy} onClick={() => void run(() => tasksApi.request(task, currentId), 'Assignment requested.')}>Request task</button>}
                  {task.assignmentRequests.length > 0 && task.status !== 'Completed' &&
                    <select aria-label={'Assign ' + task.title} value="" disabled={busy} onChange={e => { if (e.target.value) void run(() => tasksApi.assign(task, e.target.value), 'Task assigned.'); }}>
                      <option value="">Assign requester…</option>{task.assignmentRequests.map(id => <option key={id} value={id}>{name(id)}</option>)}</select>}
                  {task.assigneeId && task.status !== 'Completed' && <select aria-label={'Transfer ' + task.title}
                    value="" disabled={busy} onChange={e => { if (e.target.value) void run(() => tasksApi.assign(task, e.target.value), 'Task transferred.'); }}>
                    <option value="">Transfer to…</option>{members.filter(x => x.userId !== task.assigneeId).map(x => <option key={x.userId} value={x.userId}>{x.name}</option>)}</select>}
                  {task.assigneeId && <button disabled={busy} onClick={() => void run(() => tasksApi.unassign(task), 'Task unassigned.')}>Unassign</button>}
                  <select aria-label={'Status for ' + task.title} value={task.status} disabled={busy} onChange={e =>
                    void run(() => tasksApi.status(task, e.target.value), 'Status updated.')}>
                    {statuses.map(s => <option key={s} value={s} disabled={s === 'In Progress' && !task.assigneeId}>{s}</option>)}</select>
                  <button disabled={busy} onClick={() => void run(() => tasksApi.help(task, !task.needsHelp), 'Help flag updated.')}>{task.needsHelp ? 'Clear help' : 'Need help'}</button>
                  <button disabled={busy} onClick={() => { setEditing(task); setShowTaskForm(true); }}>Edit</button>
                  <button className="danger" disabled={busy} onClick={() => {
                    if (window.confirm('Delete this task?')) void run(() => tasksApi.delete(task), 'Task deleted.');
                  }}>Delete</button>
                </div>
              </article>)}
              {!visible.some(x => x.status === status) && <div className="column-empty">No tasks here yet.</div>}
            </div>)}</div>
          </>}
        </section>
      </div>}
    </main>
    {showTaskForm && selected && <div className="modal-backdrop" onMouseDown={() => setShowTaskForm(false)}>
      <div className="modal panel" role="dialog" aria-modal="true" aria-label={editing ? 'Edit task' : 'New task'} onMouseDown={e => e.stopPropagation()}>
        <div className="modal-title"><div><p className="eyebrow">{selected.projectName}</p><h2>{editing ? 'Edit task' : 'New task'}</h2></div>
          <button className="quiet" onClick={() => setShowTaskForm(false)}>✕</button></div>
        <form onSubmit={e => { e.preventDefault(); void saveTask(new FormData(e.currentTarget)); }}>
          <label>Task title<input name="title" required maxLength={150} defaultValue={editing?.title || ''} placeholder="What needs to be done?" /></label>
          <label>Description<textarea name="description" rows={4} defaultValue={editing?.description || ''} /></label>
          <div className="form-row"><label>Deadline<input name="deadline" type="date" defaultValue={dateInput(editing?.deadline || null)} /></label>
            <label>Priority<select name="priority" defaultValue={editing?.priority || 'Medium'}>
              <option>Low</option><option>Medium</option><option>High</option></select></label></div>
          <div className="modal-footer"><button type="button" className="quiet" onClick={() => setShowTaskForm(false)}>Cancel</button>
            <button disabled={busy}>{editing ? 'Save changes' : 'Create task'}</button></div>
        </form>
      </div></div>}
  </div>;
}
