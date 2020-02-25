﻿var $url = '/admin/settings/sitesTemplates';
var $urlUpload = $apiUrl + '/admin/settings/sitesTemplates/actions/upload';

var data = utils.initData({
  pageType: null,
  siteTemplateInfoList: null,
  fileNameList: null,
  siteTemplateUrl: null,
  siteAddPermission: false,

  uploadPanel: false,
  uploadLoading: false,
  uploadList: []
});

var error = null;

var methods = {
  getConfig: function () {
    var $this = this;

    $api.get($url).then(function (response) {
      var res = response.data;

      $this.siteTemplateInfoList = res.siteTemplateInfoList;
      $this.fileNameList = res.fileNameList;
      $this.siteTemplateUrl = res.siteTemplateUrl;
      $this.siteAddPermission = res.siteAddPermission;
    }).catch(function (error) {
      utils.error($this, error);
    }).then(function () {
      utils.loading($this, false);
    });
  },

  btnCreateClick: function(row) {
    location.href = 'sitesAdd.cshtml?type=create&createType=local&createTemplateId=' + row.directoryName;
  },

  btnEditClick: function(row) {
    location.href = 'adminRoleAdd.cshtml?roleId=' + row.id;
  },

  btnDownloadClick: function(row) {
    window.open('/' + this.siteTemplateUrl + '/' + row.directoryName + '.zip');
  },

  btnZipClick: function(row) {
    var $this = this;

    utils.loading(this, true);
    $api.post($url + '/actions/zip', {
      directoryName: row.directoryName
    }).then(function (response) {
      var res = response.data;

      $this.$message.success('站点模板压缩成功！');

      row.fileExists = true;
    }).catch(function (error) {
      utils.error($this, error);
    }).then(function () {
      utils.loading($this, false);
    });
  },

  btnDeleteClick: function (row) {
    var $this = this;

    utils.alertDelete({
      title: '删除站点模板',
      text: '此操作将会删除此站点模板“' + row.siteTemplateName + '”，确认吗？',
      callback: function () {
        $this.apiDelete(row);
      }
    });
  },

  apiDelete: function (row) {
    var $this = this;

    utils.loading(this, true);
    $api.delete($url, {
      data: {
        directoryName: row.directoryName
      }
    }).then(function (response) {
      var res = response.data;

      $this.$message.success('站点模板删除成功！');

      $this.siteTemplateInfoList.splice($this.siteTemplateInfoList.indexOf(row.directoryName), 1);
    }).catch(function (error) {
      utils.error($this, error);
    }).then(function () {
      utils.loading($this, false);
    });
  },

  btnUnZipClick: function(row) {
    var $this = this;

    utils.loading(this, true);
    $api.post($url + '/actions/unZip', {
      fileName: row
    }).then(function (response) {
      var res = response.data;

      $this.$notify.success({
        title: '成功',
        message: '站点模板解压成功！'
      });

      $this.siteTemplateInfoList = res.siteTemplateInfoList;
      $this.fileNameList = res.fileNameList;
    }).catch(function (error) {
      utils.error($this, error);
    }).then(function () {
      utils.loading($this, false);
    });
  },

  btnDownloadFileClick: function(row) {
    window.open('/' + this.siteTemplateUrl + '/' + row);
  },

  btnDeleteFileClick: function (row) {
    var $this = this;

    utils.alertDelete({
      title: '删除站点模板压缩包',
      text: '此操作将会删除此站点模板压缩包“' + row + '”，确认吗？',
      callback: function () {
        $this.apiDeleteFile(row);
      }
    });
  },

  apiDeleteFile: function (row) {
    var $this = this;

    utils.loading(this, true);
    $api.delete($url, {
      data: {
        fileName: row
      }
    }).then(function (response) {
      var res = response.data;

      $this.$message.success('站点模板压缩包删除成功！');

      $this.fileNameList.splice($this.fileNameList.indexOf(row), 1);
    }).catch(function (error) {
      utils.error($this, error);
    }).then(function () {
      utils.loading($this, false);
    });
  },

  btnUploadClick: function() {
    this.uploadPanel = true;
  },

  uploadBefore(file) {
    var isZIP = file.type === 'application/x-zip-compressed';
    if (!isZIP) {
      this.$message.error('上传站点模板只能是 ZIP 格式!');
    }
    return isZIP;
  },

  uploadProgress: function() {
    utils.loading(this, true);
  },

  uploadSuccess: function(res, file) {
    utils.loading(this, false);
    this.siteTemplateInfoList = res.siteTemplateInfoList;
    this.fileNameList = res.fileNameList;
    this.siteTemplateUrl = res.siteTemplateUrl;
    this.uploadPanel = false;
    this.$message.success('站点模板上传成功！');
  },

  uploadError: function(err) {
    utils.loading(this, false);
    var error = JSON.parse(err.message);
    this.$message.error(error.message);
  }
};

var $vue = new Vue({
  el: '#main',
  data: data,
  methods: methods,
  created: function () {
    this.getConfig();
  }
});