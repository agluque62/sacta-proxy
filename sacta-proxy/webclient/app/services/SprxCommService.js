/** */
angular
    .module('sacta_proxy')
    .factory('$serv', function ($q, $http) {
        return {
            alive: function () {
                return remoteGet(rest_url_alive);
            }
            , incidents: function () {
                return remoteGet(rest_url_inci);
            }
            , status: function () {
                return remoteGet(rest_url_std);
            }
            , config: function () {
                return remoteGet(rest_url_conf);
            }
            , config_save: function (cfg) {
                return remotePost(rest_url_conf, cfg);
            }
            , logs_get: function () {
                return remoteGet(rest_url_logs);
            }
            , logout: function () {
                return remotePost(rest_url_logout);
            }
        };

        //
        function remoteGet(url) {
            return $http.get(normalizeUrl(url));
        }

        //
        function remotePost(url, data) {
            return $http.post(normalizeUrl(url), data);
        }

        //
        function remotePut(url, data) {
            return $http.put(normalizeUrl(url), data);
        }

        //
        function remoteDel(url, data) {
            return $http.delete(normalizeUrl(url));
        }

        //
        function normalizeUrl(url) {
            if (Simulate == false)
                return url;
            return "/simulate" + url + ".json";
        }

    });

