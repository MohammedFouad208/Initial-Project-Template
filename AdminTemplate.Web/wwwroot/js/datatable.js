/* ============================================================
   AppDataTable — Generic server-side DataTable wrapper
   Depends on: jQuery, DataTables.net 1.13+, Bootstrap 5
   ============================================================ */
(function (window) {
  "use strict";

  var AppDataTable = {};

  /**
   * Initialise a server-side DataTable.
   *
   * @param {object} config
   * @param {string} config.tableId          - id of <table> element (no #)
   * @param {string} config.ajaxUrl          - server-side data endpoint
   * @param {Array}  config.columns          - DataTables column definitions
   * @param {object} config.permissions      - { canCreate, canUpdate, canDelete }
   * @param {string} [config.createUrl]      - URL for Create button
   * @param {string} [config.editUrl]        - URL template with :id placeholder
   * @param {string} [config.deleteUrl]      - URL template with :id placeholder
   * @param {string} [config.objectName]     - human label for delete modal ("User")
   * @param {object} [config.messages]       - override default UI strings
   */
  AppDataTable.init = function (config) {
    var defaults = {
      messages: {
        emptyTable:  "No records found.",
        loadError:   "Failed to load data.",
        deleteBody:  "Are you sure you want to delete this {objectName}? This action cannot be undone.",
        errorTitle:  "Something went wrong while loading data."
      }
    };

    config.messages = Object.assign({}, defaults.messages, config.messages || {});

    var $table  = $("#" + config.tableId);
    if (!$table.length) { return; }

    var permissions = config.permissions || {};

    // ----------------------------------------------------------
    // Build column list — append actions column when needed
    // ----------------------------------------------------------
    var columns = (config.columns || []).map(function (col) {
      var base = Object.assign({}, col);
      var userRender = base.render;
      base.render = function (data, type, row, meta) {
        if (type === "display") {
          if (data === null || data === undefined || data === "") {
            return userRender ? userRender(data, type, row, meta) : "—";
          }
        }
        return userRender ? userRender(data, type, row, meta) : data;
      };
      return base;
    });

    var hasActions = config.detailsUrl ||
                     (permissions.canUpdate && config.editUrl) ||
                     (permissions.canDelete && config.deleteUrl);

    if (hasActions) {
      columns.push({
        data:       null,
        title:      "Actions",
        orderable:  false,
        searchable: false,
        render: function (data, type, row) {
          var html = '<div class="d-flex gap-1">';
          if (config.detailsUrl) {
            var detailsHref = config.detailsUrl.replace(":id", row.id);
            html += '<a href="' + detailsHref + '" class="btn btn-outline-primary btn-sm" title="Details">' +
                    '<i class="fa-solid fa-eye"></i></a>';
          }
          if (permissions.canUpdate && config.editUrl) {
            var editHref = config.editUrl.replace(":id", row.id);
            html += '<a href="' + editHref + '" class="btn btn-outline-secondary btn-sm" title="Edit">' +
                    '<i class="fa-solid fa-pen-to-square"></i></a>';
          }
          if (permissions.canDelete && config.deleteUrl) {
            var delUrl = config.deleteUrl.replace(":id", row.id);
            var label  = row.fullName || row.name || row.id;
            html += '<button type="button" class="btn btn-outline-danger btn-sm dt-delete-btn" ' +
                    'data-delete-url="' + delUrl + '" data-record-id="' + row.id + '" ' +
                    'data-record-label="' + _escapeHtml(label) + '" title="Delete">' +
                    '<i class="fa-solid fa-trash"></i></button>';
          }
          html += "</div>";
          return html;
        }
      });
    }

    // ----------------------------------------------------------
    // Initialise DataTables
    // ----------------------------------------------------------
    var table = $table.DataTable({
      serverSide:  true,
      processing:  false,
      ajax: {
        url:  config.ajaxUrl,
        type: "GET",
        data: function (d) {
          var params = {
            draw:          d.draw,
            start:         d.start,
            length:        d.length,
            sortColumn:    d.order && d.order[0] ? d.order[0].column : 0,
            sortDirection: d.order && d.order[0] ? d.order[0].dir : "asc"
          };
          // Per-field filters
          if (config.searchFields && config.searchFields.length) {
            config.searchFields.forEach(function (f) {
              var val = $("#dt-filter-" + f.key).val();
              if (val) params["Filters[" + f.key + "]"] = val;
            });
          }
          return params;
        },
        error: function (xhr) {
          _showError($table, config.messages.errorTitle, table);
        }
      },
      columns:  columns,
      language: {
        emptyTable: _buildEmptyState(config.messages.emptyTable),
        processing: '<span class="fa-solid fa-spinner fa-spin me-1"></span> Loading…'
      },
      dom: "<'dt-custom-toolbar'>" +
           "<'row'<'col-12't>>" +
           "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
      pageLength: 10,
      responsive: true
    });

    // ----------------------------------------------------------
    // Build toolbar (search + length + Create button)
    // ----------------------------------------------------------
    _buildToolbar($table, table, config, permissions);

    // ----------------------------------------------------------
    // Delete modal wiring
    // ----------------------------------------------------------
    if (permissions.canDelete && config.deleteUrl) {
      $table.on("click", ".dt-delete-btn", function () {
        var $btn      = $(this);
        var deleteUrl = $btn.data("delete-url");
        var label     = $btn.data("record-label");
        var objectName = config.objectName || "record";

        var bodyText = config.messages.deleteBody
          .replace("{objectName}", objectName)
          .replace("{label}", label);

        var $modal  = $("#deleteConfirmModal");
        var $body   = $("#deleteConfirmBody");
        var $confirm = $("#deleteConfirmBtn");

        if ($modal.length) {
          $body.text(bodyText);
          $confirm.data("delete-url", deleteUrl);
          bootstrap.Modal.getOrCreateInstance($modal[0]).show();
        } else {
          // Fallback if modal partial is missing
          if (window.confirm(bodyText)) {
            _doDelete(deleteUrl, table, $table, config.messages.errorTitle);
          }
        }
      });

      $(document).on("click", "#deleteConfirmBtn", function () {
        var deleteUrl = $(this).data("delete-url");
        var $modal = $("#deleteConfirmModal");
        bootstrap.Modal.getOrCreateInstance($modal[0]).hide();
        _doDelete(deleteUrl, table, $table, config.messages.errorTitle);
      });
    }

    return table;
  };

  // ----------------------------------------------------------
  // Private helpers
  // ----------------------------------------------------------
  function _buildToolbar($table, table, config, permissions) {
    var $wrapper = $table.closest(".dataTables_wrapper");
    if (!$wrapper.length) { $wrapper = $table.parent(); }

    var $toolbar = $wrapper.find(".dt-custom-toolbar");
    $toolbar.html("");

    // Row 1: search fields (2 per row) + length selector
    if (config.searchFields && config.searchFields.length) {
      var $searchRow = $('<div class="row g-2 mb-2"></div>');

      config.searchFields.forEach(function (f) {
        var $col = $('<div class="col-12 col-sm-6"></div>');
        if (f.type === "select" && f.dataUrl) {
          // Dropdown filter — populated via AJAX
          var $select = $('<select class="form-select form-select-sm dt-field-search dt-field-select"></select>')
            .attr("id",         "dt-filter-" + f.key)
            .attr("aria-label", "Filter " + _escapeHtml(f.label));
          $select.append($('<option value="">— ' + _escapeHtml(f.label) + ' —</option>'));
          // Load options
          $.getJSON(f.dataUrl, function (items) {
            $.each(items, function (_, item) {
              $select.append($('<option></option>').val(item.id).text(item.text));
            });
          });
          $col.append($select);
        } else {
          $col.append(
            $('<div class="input-group input-group-sm"></div>').append(
              $('<span class="input-group-text"><i class="fa-solid fa-magnifying-glass"></i></span>'),
              $('<input type="search" class="form-control dt-field-search">')
                .attr("id",          "dt-filter-" + f.key)
                .attr("placeholder", _escapeHtml(f.label) + "…")
                .attr("aria-label",  "Search " + _escapeHtml(f.label))
            )
          );
        }
        $searchRow.append($col);
      });

      $toolbar.append($searchRow);
    }

    // Row 2: length selector left, Create button right
    var $bottomRow = $('<div class="d-flex justify-content-between align-items-center mb-3"></div>');

    $bottomRow.append(
      $('<select class="form-select form-select-sm dt-length-select" style="width:auto">' +
          '<option value="10">10</option>' +
          '<option value="25">25</option>' +
          '<option value="50">50</option>' +
          '<option value="100">100</option>' +
        '</select>')
    );

    if (permissions.canCreate && config.createUrl) {
      $bottomRow.append(
        $('<a class="btn btn-primary btn-sm"></a>')
          .attr("href", config.createUrl)
          .html('<i class="fa-solid fa-plus me-1"></i>Create')
      );
    }

    $toolbar.append($bottomRow);

    // Wire per-field text search with debounce
    var searchTimer;
    $wrapper.on("input", ".dt-field-search", function () {
      clearTimeout(searchTimer);
      searchTimer = setTimeout(function () {
        table.ajax.reload(null, false);
      }, 400);
    });

    // Wire select search (immediate)
    $wrapper.on("change", ".dt-field-select", function () {
      table.ajax.reload(null, false);
    });

    // Wire length
    $wrapper.on("change", ".dt-length-select", function () {
      table.page.len(parseInt($(this).val(), 10)).draw();
    });
  }

  function _buildEmptyState(message) {
    return '<div class="dt-empty-state py-4 text-center" style="color:var(--text-secondary, #6c757d)">' +
           '<i class="fa-solid fa-inbox fa-2x mb-2 d-block"></i>' +
           '<span>' + _escapeHtml(message) + '</span>' +
           '</div>';
  }

  function _showError($table, title, table) {
    var $wrapper = $table.closest(".dataTables_wrapper");
    if (!$wrapper.length) { $wrapper = $table.parent(); }

    var $existing = $wrapper.find(".dt-error-panel");
    if ($existing.length) { return; }

    var $panel = $(
      '<div class="alert dt-error-panel mb-3" ' +
           'style="border-inline-start:4px solid var(--danger,#dc3545);background:var(--danger-light,#fff5f5)">' +
        '<i class="fa-solid fa-circle-exclamation me-2"></i>' +
        '<strong>' + _escapeHtml(title) + '</strong>' +
        '<button type="button" class="btn btn-outline-danger btn-sm ms-3 dt-retry-btn">' +
          '<i class="fa-solid fa-rotate-right me-1"></i>Retry' +
        '</button>' +
      '</div>'
    );

    $wrapper.prepend($panel);

    $panel.on("click", ".dt-retry-btn", function () {
      $panel.remove();
      table.ajax.reload();
    });
  }

  function _doDelete(deleteUrl, table, $table, errorTitle) {
    var token = $('input[name="__RequestVerificationToken"]').val();
    if (!token) {
      console.warn("AppDataTable: CSRF token not found — delete aborted.");
      return;
    }

    fetch(deleteUrl, {
      method:  "POST",
      headers: { "RequestVerificationToken": token }
    })
    .then(function (res) {
      if (res.ok) {
        table.ajax.reload(null, false);
        document.dispatchEvent(new CustomEvent("dtable:deleted", { detail: { url: deleteUrl } }));
      } else {
        _showError($table, errorTitle, table);
      }
    })
    .catch(function () {
      _showError($table, errorTitle, table);
    });
  }

  function _escapeHtml(str) {
    if (!str) { return ""; }
    return String(str)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  window.AppDataTable = AppDataTable;

}(window));
