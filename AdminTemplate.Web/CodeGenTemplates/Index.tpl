@{
    ViewData["Title"] = "{{ entity.name }}";
}

<div class="page-header d-flex align-items-start justify-content-between flex-wrap gap-3">
    <div>
        <h1 class="mb-1">{{ entity.name }}</h1>
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb">
                <li class="breadcrumb-item">
                    <a asp-controller="Dashboard" asp-action="Index" class="text-decoration-none" style="color:var(--primary)">Home</a>
                </li>
                <li class="breadcrumb-item active" aria-current="page">{{ entity.name }}</li>
            </ol>
        </nav>
    </div>
</div>

@if (TempData["Success"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert"
         style="border-inline-start:4px solid var(--success)">
        <i class="fa-solid fa-circle-check me-2"></i>@TempData["Success"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<div class="card table-card">
    <div class="card-body">
        <table id="{{ entity.name | string.downcase }}Table" class="table table-hover w-100"></table>
    </div>
</div>

@Html.AntiForgeryToken()
<partial name="_DeleteConfirmModal" />

@section Scripts {
<script>
AppDataTable.init({
    tableId: '{{ entity.name | string.downcase }}Table',
    ajaxUrl: '@Url.Action("GetData", "{{ entity.name }}")',
    searchFields: [
        {{- for col in entity.columns }}
        {{- if col.use_in_search }}
        { key: '{{ col.name | string.slice 0 1 | string.downcase }}{{ col.name | string.slice 1 }}', label: '{{ col.name }}' },
        {{- end }}
        {{- end }}
        {{- for rel in entity.relations }}
        { key: 'rel_{{ rel.navigation_property_name | string.slice 0 1 | string.downcase }}{{ rel.navigation_property_name | string.slice 1 }}', label: '{{ rel.related_entity_name }}', type: 'select', dataUrl: '@Url.Action("GetLookup{{ rel.related_entity_name }}", "{{ entity.name }}")' },
        {{- end }}
    ],
    columns: [
        {{- for col in entity.columns }}
        {{- if col.show_in_list }}
        {
            data: '{{ col.name | string.slice 0 1 | string.downcase }}{{ col.name | string.slice 1 }}',
            title: '{{ col.name }}',
            {{- if !col.use_in_search }}
            searchable: false,
            {{- end }}
            {{- if col.data_type == "datetime" }}
            render: function (val, type) {
                if (!val) { return '—'; }
                if (type !== 'display') { return val; }
                return new Date(val).toLocaleDateString();
            }
            {{- else if col.data_type == "bool" }}
            render: function (val, type) {
                if (type !== 'display') { return val ? 'Yes' : 'No'; }
                return val
                    ? '<span class="badge badge-soft badge-soft-success">Yes</span>'
                    : '<span class="badge badge-soft badge-soft-secondary">No</span>';
            }
            {{- end }}
        },
        {{- end }}
        {{- end }}
        {{- for rel in entity.relations }}
        {
            data: '{{ rel.navigation_property_name | string.slice 0 1 | string.downcase }}{{ rel.navigation_property_name | string.slice 1 }}{{ rel.display_column }}',
            title: '{{ rel.related_entity_name }}',
            orderable: false,
            searchable: false
        },
        {{- end }}
    ],
    permissions: {
        canCreate: @Json.Serialize(ViewBag.CanCreate),
        canUpdate: @Json.Serialize(ViewBag.CanUpdate),
        canDelete: @Json.Serialize(ViewBag.CanDelete)
    },
    createUrl:  '@Url.Action("Create",  "{{ entity.name }}")',
    detailsUrl: '@Url.Action("Details", "{{ entity.name }}")/:id',
    editUrl:    '@Url.Action("Edit",    "{{ entity.name }}")/:id',
    deleteUrl:  '@Url.Action("Delete",  "{{ entity.name }}")/:id',
    objectName: '{{ entity.name }}'
});
</script>
}
