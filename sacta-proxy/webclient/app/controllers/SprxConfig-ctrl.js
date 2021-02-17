angular.module("sacta_proxy")
    .controller("SprxConfigCtrl", function ($scope, $interval, $serv, $lserv) {

        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;
        ctrl.config = undefined;
        ctrl.selecteddep = "0";
        ctrl.dep = {
            cfg: null,
            showselect: false,
            showid: false,
            showsectdata: false
        };
        ctrl.topage = (page) => {
            if (page == 1) {
                if (ctrl.config) {
                    ctrl.dep.cfg = ctrl.config.Psi;
                    ctrl.dep.showselect= false;
                    ctrl.dep.showid = false;
                    ctrl.dep.showsectdata = false;
                }
            }
            else if (page == 2) {
                if (ctrl.config) {
                    ctrl.dep.cfg = ctrl.config.Dependencies[ctrl.selecteddep];
                    ctrl.dep.showselect = true;
                    ctrl.dep.showid = true;
                    ctrl.dep.showsectdata = true;
                }
            }
            ctrl.pagina = page;
        }
        ctrl.update = () => {
            alertify.success("Updating config..")

        };
        ctrl.reset = () => {
            load_config(() => {
                /** reinicia los estados del form de configuracion */
                $scope.cfgform.$setPristine();
                ctrl.topage(ctrl.pagina);
            });
        };

        function load_config(sync) {
            $serv.config((config) => {
                if (config.res == "ok") {
                    ctrl.config = config.cfg;
                    if (sync) sync();
                }
                else {
                    // todo
                }
            });
        }

        /** */
        $scope.$on('$viewContentLoaded', function () {
            load_config();
        });
        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {

        });
    });
