(() => {
    document.querySelectorAll('[data-team-picker]').forEach(root => {
        const data = JSON.parse(root.querySelector('[data-team-data]').textContent);
        const main = document.querySelector('[name="Project.DepartmentId"]');
        const own = root.querySelector('[data-own-team]');
        const extras = root.querySelector('[data-extra-teams]');
        const choice = root.querySelector('[data-extra-choice]');
        const add = root.querySelector('[data-add-team]');
        const departments = [...data.departments];
        if (data.users.some(u => u.departmentId === -1)) departments.push({id: -1, name: 'Departmanı olmayan kullanıcılar'});
        let sequence = 0;
        const selections = () => [...root.querySelectorAll('[name="MemberUserIds"] option:checked')].map(o => o.value);
        const populatePeople = (select, deptId, selected) => {
            select.replaceChildren();
            data.users.filter(u => u.departmentId === deptId).forEach(u => {
                const option = new Option(u.name, u.id, false, selected.includes(u.id));
                select.add(option);
            });
        };
        const makePeople = (container, deptId, selected) => {
            const label = document.createElement('label');
            const select = document.createElement('select');
            select.id = `team-people-${++sequence}`; label.htmlFor = select.id;
            label.textContent = 'Katılımcılar (çoklu seçim)';
            select.name = 'MemberUserIds'; select.multiple = true; select.className = 'form-control';
            populatePeople(select, deptId, selected);
            container.append(label, select);
            return select;
        };
        const refreshOptions = () => {
            const taken = [...extras.querySelectorAll('[data-department]')].map(s => s.value);
            extras.querySelectorAll('[data-department]').forEach(s => [...s.options].forEach(o => {
                o.disabled = o.value !== s.value && taken.includes(o.value);
            }));
        };
        const addGroup = (deptId, selected = []) => {
            const available = departments.filter(d => d.id !== Number(main.value));
            const used = [...extras.querySelectorAll('[data-department]')].map(s => Number(s.value));
            deptId ??= available.find(d => !used.includes(d.id))?.id;
            if (deptId === undefined) return;
            const group = document.createElement('div'); group.className = 'form-field';
            const label = document.createElement('label'); label.textContent = 'Katılımcı departman';
            const dept = document.createElement('select'); dept.dataset.department = ''; dept.className = 'form-control';
            dept.id = `team-department-${++sequence}`; label.htmlFor = dept.id;
            available.forEach(d => dept.add(new Option(d.name, d.id, false, d.id === deptId)));
            group.append(label, dept);
            const people = makePeople(group, deptId, selected);
            dept.addEventListener('change', () => { populatePeople(people, Number(dept.value), []); refreshOptions(); });
            const remove = document.createElement('button'); remove.type = 'button'; remove.textContent = 'Departmanı kaldır';
            remove.className = 'project-action-button project-action-secondary';
            remove.addEventListener('click', () => { group.remove(); refreshOptions(); });
            group.append(remove); extras.append(group); refreshOptions();
        };
        const render = selected => {
            own.replaceChildren(); extras.replaceChildren();
            makePeople(own, Number(main.value), selected);
            const otherIds = [...new Set(data.users.filter(u => selected.includes(u.id) && u.departmentId !== Number(main.value)).map(u => u.departmentId))];
            otherIds.forEach(id => addGroup(id, selected));
            choice.value = otherIds.length ? 'yes' : 'no';
            extras.hidden = add.hidden = choice.value !== 'yes';
        };
        choice.addEventListener('change', () => {
            extras.hidden = add.hidden = choice.value !== 'yes';
            if (choice.value === 'no') extras.replaceChildren();
            else if (!extras.children.length) addGroup();
        });
        add.addEventListener('click', () => addGroup());
        main.addEventListener('change', () => render(selections()));
        render(data.selected);
    });
})();
