@using AdminTemplate.Web.Models.ViewModels
@model {{ entity.name }}DetailsViewModel
@{
    ViewData["Title"] = "{{ entity.name }} Details";
    var canUpdate = ViewBag.CanUpdate == true;
    var canDelete = ViewBag.CanDelete == true;
}

<div class="page-header d-flex align-items-start justify-content-between flex-wrap gap-3">
    <div>
        <h1 class="mb-1">
            <i class="fa-solid fa-file-lines me-2 text-primary"></i>{{ entity.name }} Details
        </h1>
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb">
                <li class="breadcrumb-item">
                    <a asp-controller="Dashboard" asp-action="Index" class="text-decoration-none" style="color:var(--primary)">Home</a>
                </li>
                <li class="breadcrumb-item">
                    <a asp-action="Index" class="text-decoration-none" style="color:var(--primary)">{{ entity.name }}</a>
                </li>
                <li class="breadcrumb-item active" aria-current="page">Details</li>
            </ol>
        </nav>
    </div>
    <div class="d-flex gap-2 flex-wrap">
        @if (canUpdate)
        {
            <a asp-action="Edit" asp-route-id="@Model.Id" class="btn btn-outline-secondary">
                <i class="fa-solid fa-pen-to-square me-1"></i>Edit
            </a>
        }
        @if (canDelete)
        {
            <button type="button" class="btn btn-outline-danger" id="btnDeleteRecord"
                    data-id="@Model.Id">
                <i class="fa-solid fa-trash me-1"></i>Delete
            </button>
        }
        <a asp-action="Index" class="btn btn-outline-secondary">
            <i class="fa-solid fa-arrow-left me-1"></i>Back
        </a>
    </div>
</div>

@if (TempData["Success"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert"
         style="border-inline-start:4px solid var(--success)">
        <i class="fa-solid fa-circle-check me-2"></i>@TempData["Success"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

<div class="row g-4">

    {{- # ── Main fields card ─────────────────────────────────────────────────── }}
    <div class="col-lg-8">
        <div class="card h-100">
            <div class="card-header fw-semibold">
                <i class="fa-solid fa-circle-info me-2 text-primary"></i>{{ entity.name }} Information
            </div>
            <div class="card-body">
                <div class="row g-4">
                    {{- for col in entity.columns }}
                    {{- if col.show_in_list }}
                    <div class="col-sm-6">
                        <div class="d-flex align-items-start gap-3">
                            <div class="flex-shrink-0 rounded-circle d-flex align-items-center justify-content-center"
                                 style="width:38px;height:38px;background:var(--primary-light,#e8f0fe)">
                                {{- if col.data_type == "bool" }}
                                <i class="fa-solid fa-toggle-on text-primary"></i>
                                {{- else if col.data_type == "datetime" }}
                                <i class="fa-solid fa-calendar-days text-primary"></i>
                                {{- else if col.data_type == "int" || col.data_type == "long" || col.data_type == "decimal" || col.data_type == "double" }}
                                <i class="fa-solid fa-hashtag text-primary"></i>
                                {{- else }}
                                <i class="fa-solid fa-tag text-primary"></i>
                                {{- end }}
                            </div>
                            <div class="min-w-0">
                                <div class="text-muted small mb-1">{{ col.name }}</div>
                                <div class="fw-semibold text-break">
                                    {{- if col.data_type == "bool" }}
                                    @if (Model.{{ col.name }})
                                    {
                                        <span class="badge badge-soft badge-soft-success"><i class="fa-solid fa-check me-1"></i>Yes</span>
                                    }
                                    else
                                    {
                                        <span class="badge badge-soft badge-soft-secondary"><i class="fa-solid fa-xmark me-1"></i>No</span>
                                    }
                                    {{- else if col.data_type == "datetime" }}
                                    @(Model.{{ col.name }} == default ? "—" : Model.{{ col.name }}.ToString("dd MMM yyyy, HH:mm"))
                                    {{- else }}
                                    @(Model.{{ col.name }} is null || Model.{{ col.name }}.ToString() == "" ? "—" : Model.{{ col.name }}.ToString())
                                    {{- end }}
                                </div>
                            </div>
                        </div>
                    </div>
                    {{- end }}
                    {{- end }}
                </div>
            </div>
        </div>
    </div>

    {{- # ── Sidebar: meta + relations ──────────────────────────────────────── }}
    <div class="col-lg-4 d-flex flex-column gap-4">

        {{- # Meta card }}
        <div class="card">
            <div class="card-header fw-semibold">
                <i class="fa-solid fa-clock-rotate-left me-2 text-secondary"></i>Record Info
            </div>
            <div class="card-body p-0">
                <ul class="list-group list-group-flush">
                    <li class="list-group-item d-flex justify-content-between align-items-center py-3">
                        <span class="text-muted small"><i class="fa-solid fa-fingerprint me-2"></i>ID</span>
                        <code class="small text-break ms-2" style="max-width:60%">@Model.Id</code>
                    </li>
                    <li class="list-group-item d-flex justify-content-between align-items-center py-3">
                        <span class="text-muted small"><i class="fa-solid fa-calendar-plus me-2"></i>Created At</span>
                        <span class="fw-semibold small">@Model.CreatedAt.ToString("dd MMM yyyy, HH:mm")</span>
                    </li>
                </ul>
            </div>
        </div>

        {{- if entity.relations.size > 0 }}
        {{- # Relations card }}
        <div class="card">
            <div class="card-header fw-semibold">
                <i class="fa-solid fa-link me-2 text-secondary"></i>Relations
            </div>
            <div class="card-body p-0">
                <ul class="list-group list-group-flush">
                    {{- for rel in entity.relations }}
                    <li class="list-group-item d-flex justify-content-between align-items-center py-3">
                        <span class="text-muted small">
                            <i class="fa-solid fa-arrow-right-to-bracket me-2"></i>{{ rel.related_entity_name }}
                        </span>
                        <span class="fw-semibold small ms-2">@Model.{{ rel.navigation_property_name }}Display</span>
                    </li>
                    {{- end }}
                </ul>
            </div>
        </div>
        {{- end }}

    </div>
</div>

@Html.AntiForgeryToken()
<partial name="_DeleteConfirmModal" />

@section Scripts {
<script>
(function () {
    var _token = $('input[name="__RequestVerificationToken"]').val();

    @if (canDelete)
    {
        <text>
    $('#btnDeleteRecord').on('click', function () {
        var id = $(this).data('id');
        var deleteUrl = '@Url.Action("Delete", "{{ entity.name }}")/' + id;
        var $modal   = $('#deleteConfirmModal');
        var $body    = $('#deleteConfirmBody');
        var $confirm = $('#deleteConfirmBtn');

        if ($modal.length) {
            $body.text('Are you sure you want to delete this {{ entity.name }}? This action cannot be undone.');
            $confirm.data('delete-url', deleteUrl);
            bootstrap.Modal.getOrCreateInstance($modal[0]).show();
        } else if (window.confirm('Delete this {{ entity.name }}?')) {
            _doDelete(deleteUrl);
        }
    });

    $(document).on('click', '#deleteConfirmBtn', function () {
        var deleteUrl = $(this).data('delete-url');
        bootstrap.Modal.getOrCreateInstance($('#deleteConfirmModal')[0]).hide();
        _doDelete(deleteUrl);
    });

    function _doDelete(url) {
        fetch(url, { method: 'POST', headers: { 'RequestVerificationToken': _token } })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (res.success) {
                    window.location.href = '@Url.Action("Index", "{{ entity.name }}")';
                } else {
                    alert(res.message || 'Delete failed.');
                }
            })
            .catch(function () { alert('An unexpected error occurred.'); });
    }
        </text>
    }
}());
</script>
}
