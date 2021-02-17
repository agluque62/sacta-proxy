/** */
angular
    .module('sacta_proxy')
    .factory('$serv', function ($q, $http) {
        return {
            alive: function (deliver) {
                Get(rest_url_alive, deliver);
            }
            , incidents: function (deliver) {
                Get(rest_url_inci, deliver);
            }
            , status: function (deliver) {
                Get(rest_url_std, deliver);
            }
            , config: function (deliver) {
                Get(rest_url_conf, deliver);
            }
            , config_save: function (cfg, response) {
                Post(rest_url_conf, cfg, response);
            }
            , logs_get: function (deliver) {
                Get(rest_url_logs, deliver);
            }
            , logout: function () {
                return remotePost(rest_url_logout);
            }
            , version: function (deliver) {
                Get("/version", deliver);
            }
        };

        function Get(url, deliver) {
            $http.get(normalizeUrl(url)).then(
                (response) => {
                    if (response.status == 200) {
                        if ((typeof response.data) == 'object') {
                            if (deliver) deliver(response.data);
                        }
                        else {
                            // Seguramente ha vencido la sesion.
                            console.log("Sesion Vencida...");
                            window.location.href = "/login.html";
                        }
                    }
                    else {
                        // Error en el servidor.
                        console.log("Error Servidor " + response.status);
                    }
                },
                (error) => {
                    // Error en el tratamiento de la peticion.
                    console.log("Error Peticion");
                    window.open("/", "_self");
                }
            );
        }

        //
        function remoteGet(url) {
            return $http.get(normalizeUrl(url));
        }

        function Post(url, data, deliver) {
            $http.post(normalizeUrl(url), data).then(
                (response) => {
                    if (response.status == 200) {
                        if ((typeof response.data) == 'object') {
                            if (deliver) deliver(response.data);
                        }
                        else {
                            // Seguramente ha vencido la sesion.
                            console.log("Sesion Vencida...");
                            window.location.href = "/login.html";
                        }
                    }
                    else {
                        // Error en el servidor.
                        console.log("Error Servidor " + response.status);
                    }
                },
                (error) => {
                    // Error en el tratamiento de la peticion.
                    console.log("Error Peticion");
                    window.open("/", "_self");
                }
            );

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

