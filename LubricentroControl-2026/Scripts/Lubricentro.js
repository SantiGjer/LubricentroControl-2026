/* Lubricentro.js — comportamiento compartido de las pantallas (Site.Master lo carga en todas).

   1. Tablas (table.tabla-abm): orden al hacer clic en un encabezado, filtro por texto y, si la
      tabla lo pide con data-filas-por-pagina, paginado. Todo en el navegador, sobre las filas
      que ya mandó el servidor. Lo que el usuario dejó elegido (filtro, opciones, orden, página)
      sobrevive a los postbacks porque se guarda en el campo oculto hdnEstadoTablas del
      Site.Master: un GET nuevo arranca de cero, un postback lo recupera.
        - data-filtro="id": usa ese input como filtro en vez de agregar uno arriba de la tabla.
        - data-sin-filtro: tabla sin filtro ni elección de columnas (solo orden), para las grillas
          chicas de un formulario.
        - data-buscar (en una fila): texto extra que el filtro tiene en cuenta aunque no se vea.
        - Las columnas con clase "sin-orden" en el encabezado (las de Acciones) no se ordenan.
      Opciones: data-opciones-tabla="id" apunta a un contenedor .opciones-tabla con grupos
      (.grupo-opciones) de botones (.opcion). Cada grupo filtra por una columna
      (data-columna="Estado", por el texto del encabezado) o por un atributo de la fila
      (data-atributo="saldo" lee data-saldo); data-valor="" es "todos", y "A|B" acepta
      cualquiera de los dos. data-inicial elige la opción con que arranca la pantalla.
      Columnas: el botón "Columnas" deja elegir qué columnas se ven; la elección se recuerda en
      este navegador. Las columnas con clase "oculta" arrancan escondidas.
      Ver: un enlace .accion-ver con data-ver-detalle en la fila abre una ventana con todos los
      datos de la fila, incluidas las columnas escondidas; los elementos .accion-detalle de la
      fila aparecen como botones en el pie de esa ventana. Las celdas con clase "celda-titulo"
      (el nombre o el número) abren lo mismo que el .accion-ver de su fila. Si la fila tiene una
      imagen con data-imagen (la miniatura de un producto), la ventana muestra esa imagen grande.
   2. Selectores con búsqueda (.selector-busqueda): campo de texto con la lista de opciones
      desplegable y el botón de búsqueda adentro del mismo campo. Las opciones vienen del
      servidor en data-opciones ([{ "v": valor, "t": texto }]) y el valor elegido queda en el
      HiddenField de adentro. Con data-postback="true" avisa al servidor al elegir (dispara el
      ValueChanged de ese HiddenField).
   3. Modales: Lubricentro.abrirModal(id). El servidor lo llama después de un postback
      (Utilidades/Interfaz.AbrirModal) para que el formulario siga a la vista.
      Campos condicionales: data-mostrar-si="idControl=Valor" (ver iniciarCondicionales).
   4. Barra lateral: Lubricentro.restaurarMenu() reabre los grupos del menú que el usuario dejó
      abiertos (se recuerda en este navegador).
   5. Campo de imagen (.campo-imagen): muestra la imagen elegida antes de guardarla y descarta la
      que no sirve (ver iniciarCampoImagen). */
(function () {
    'use strict';

    var Lubricentro = window.Lubricentro = window.Lubricentro || {};

    var comparador = window.Intl && Intl.Collator
        ? new Intl.Collator('es', { sensitivity: 'base', numeric: true })
        : { compare: function (a, b) { return a < b ? -1 : a > b ? 1 : 0; } };

    // --- Texto -------------------------------------------------------------------------

    // Minúsculas y sin acentos, para que "perez" encuentre "Pérez".
    function normalizar(texto) {
        var t = (texto || '').toString().toLowerCase();
        if (t.normalize) t = t.normalize('NFD').replace(/[̀-ͯ]/g, '');
        return t.replace(/\s+/g, ' ').trim();
    }

    // Solo letras y números: así "20123456786" encuentra "20-12345678-6" y "1143215678"
    // encuentra "11-4321-5678".
    function compactar(texto) {
        return texto.replace(/[^a-z0-9]/g, '');
    }

    function partirEnPalabras(texto) {
        var normalizado = normalizar(texto);
        return normalizado ? normalizado.split(' ') : [];
    }

    // Todas las palabras buscadas tienen que aparecer, en cualquier orden.
    function coincide(palabras, texto, textoCompacto) {
        for (var i = 0; i < palabras.length; i++) {
            var palabra = palabras[i];
            if (texto.indexOf(palabra) >= 0) continue;
            var compacta = compactar(palabra);
            if (compacta && textoCompacto.indexOf(compacta) >= 0) continue;
            return false;
        }
        return true;
    }

    // --- Preferencias del navegador (localStorage) ---------------------------------------
    // Solo comodidades de cada persona (columnas elegidas, grupos del menú abiertos): si el
    // navegador no deja guardar, todo funciona igual con los valores por defecto.

    function leerPreferencia(clave) {
        try {
            var valor = window.localStorage.getItem('lubricentro.' + clave);
            return valor ? JSON.parse(valor) : null;
        } catch (e) {
            return null;
        }
    }

    function guardarPreferencia(clave, valor) {
        try {
            window.localStorage.setItem('lubricentro.' + clave, JSON.stringify(valor));
        } catch (e) { /* sin almacenamiento: no se recuerda */ }
    }

    // --- Estado que sobrevive a los postbacks --------------------------------------------

    function campoEstado() {
        return document.getElementById('hdnEstadoTablas');
    }

    // Va codificado (encodeURIComponent): el texto del filtro puede traer "<", y ASP.NET
    // rechaza un postback con un campo que parezca HTML (request validation).
    function leerTodoElEstado() {
        var campo = campoEstado();
        if (!campo || !campo.value) return {};
        try {
            return JSON.parse(decodeURIComponent(campo.value)) || {};
        } catch (e) {
            return {};
        }
    }

    function leerEstado(clave) {
        return leerTodoElEstado()[clave] || null;
    }

    function guardarEstado(clave, valor) {
        var campo = campoEstado();
        if (!campo) return;
        var estado = leerTodoElEstado();
        estado[clave] = valor;
        campo.value = encodeURIComponent(JSON.stringify(estado));
    }

    // --- Valores de una celda para ordenar ----------------------------------------------

    var formatoFecha = /^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:\s+(\d{1,2}):(\d{2}))?$/;

    function leerFecha(texto) {
        var m = formatoFecha.exec(texto);
        if (!m) return null;
        return Date.UTC(+m[3], +m[2] - 1, +m[1], m[4] ? +m[4] : 0, m[5] ? +m[5] : 0);
    }

    // Los importes salen con el formato regional del servidor (N2): el Site.Master publica sus
    // separadores en el <body> para no tener que adivinarlos.
    function leerNumero(texto) {
        var miles = document.body.getAttribute('data-separador-miles') || ',';
        var decimal = document.body.getAttribute('data-separador-decimal') || '.';
        var t = texto.replace(/[$\s ]/g, '');
        if (!t) return null;
        t = t.split(miles).join('');
        if (decimal !== '.') t = t.split(decimal).join('.');
        return /^-?\d+(\.\d+)?$/.test(t) ? parseFloat(t) : null;
    }

    // Decide el tipo de toda la columna mirando sus valores: si todos son fechas se ordena
    // como fecha, si todos son números como número, y si no, como texto.
    function tipoDeColumna(valores) {
        var fechas = true, numeros = true, alguno = false;
        for (var i = 0; i < valores.length; i++) {
            var v = valores[i];
            if (!v) continue;
            alguno = true;
            if (fechas && leerFecha(v) === null) fechas = false;
            if (numeros && leerNumero(v) === null) numeros = false;
            if (!fechas && !numeros) break;
        }
        if (!alguno) return 'texto';
        return fechas ? 'fecha' : numeros ? 'numero' : 'texto';
    }

    // --- Tablas ----------------------------------------------------------------------------

    function textoCelda(fila, columna) {
        var celda = fila.cells[columna];
        return celda ? celda.textContent.replace(/\s+/g, ' ').trim() : '';
    }

    function esColumnaDeAcciones(th) {
        var texto = normalizar(th.textContent);
        return texto === 'acciones' || texto === '';
    }

    function esOrdenable(th) {
        return !th.classList.contains('sin-orden') && !esColumnaDeAcciones(th);
    }

    function crearFiltro(tabla) {
        var barra = document.createElement('div');
        barra.className = 'filtro-tabla';
        var input = document.createElement('input');
        input.type = 'search';
        input.className = 'filtro-tabla-texto';
        input.placeholder = 'Filtrar…';
        input.setAttribute('aria-label', 'Filtrar la tabla');
        barra.appendChild(input);
        tabla.parentNode.insertBefore(barra, tabla);
        return input;
    }

    function iniciarTabla(tabla) {
        tabla.setAttribute('data-tabla-iniciada', '');

        var encabezado = null;
        for (var i = 0; i < tabla.rows.length; i++) {
            var celdas = tabla.rows[i].cells;
            if (celdas.length && celdas[0].tagName === 'TH') {
                encabezado = tabla.rows[i];
                break;
            }
        }
        // Una grilla vacía solo trae la fila con el texto de "no hay datos": nada que ordenar.
        if (!encabezado) return;

        var filas = [];
        for (var j = 0; j < tabla.rows.length; j++) {
            var fila = tabla.rows[j];
            if (fila !== encabezado && fila.cells.length && fila.cells[0].tagName === 'TD')
                filas.push(fila);
        }

        var columnasOrdenables = [];
        for (var c = 0; c < encabezado.cells.length; c++)
            columnasOrdenables.push(esOrdenable(encabezado.cells[c]));

        // Texto de cada fila para el filtro (sin la columna de acciones), calculado una sola vez.
        var datos = filas.map(function (f, indice) {
            var partes = [];
            for (var k = 0; k < f.cells.length; k++)
                if (columnasOrdenables[k]) partes.push(f.cells[k].textContent);
            partes.push(f.getAttribute('data-buscar') || '');
            var texto = normalizar(partes.join(' '));
            return { fila: f, indice: indice, texto: texto, compacto: compactar(texto), opciones: {} };
        });

        var t = {
            tabla: tabla,
            clave: tabla.id || ('tabla' + Array.prototype.indexOf.call(document.querySelectorAll('table.tabla-abm'), tabla)),
            encabezado: encabezado,
            datos: datos,
            columna: -1,
            direccion: 1,
            filtro: '',
            pagina: 0,
            porPagina: parseInt(tabla.getAttribute('data-filas-por-pagina'), 10) || 0,
            tipos: {},
            input: null,
            paginador: null,
            filaVacia: null,
            grupos: [],
            opciones: {},
            contador: null
        };

        var guardado = leerEstado(t.clave);
        if (guardado) {
            t.columna = typeof guardado.c === 'number' ? guardado.c : -1;
            t.direccion = guardado.d === -1 ? -1 : 1;
            t.filtro = guardado.f || '';
            t.pagina = guardado.p || 0;
        }

        // Encabezados: clic (o Enter) para ordenar; el segundo clic invierte el orden.
        Array.prototype.forEach.call(encabezado.cells, function (th, columna) {
            if (!columnasOrdenables[columna]) return;
            th.classList.add('ordenable');
            th.setAttribute('tabindex', '0');
            th.setAttribute('title', 'Ordenar por esta columna');
            var ordenar = function () {
                if (t.columna === columna) t.direccion = -t.direccion;
                else {
                    t.columna = columna;
                    t.direccion = 1;
                }
                aplicar(t);
            };
            th.addEventListener('click', ordenar);
            th.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    ordenar();
                }
            });
        });

        // Filtro: el input que indique la tabla, o uno nuevo arriba de ella.
        if (!tabla.hasAttribute('data-sin-filtro')) {
            var idFiltro = tabla.getAttribute('data-filtro');
            t.input = idFiltro ? document.getElementById(idFiltro) : crearFiltro(tabla);
        }
        if (t.input) {
            t.input.value = t.filtro;
            t.input.addEventListener('input', function () {
                t.filtro = t.input.value;
                t.pagina = 0;
                aplicar(t);
            });
            // Enter en el filtro no tiene que mandar el formulario (dispararía otro botón).
            t.input.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') e.preventDefault();
            });
        }

        iniciarOpciones(t, guardado);
        if (!tabla.hasAttribute('data-sin-filtro')) iniciarColumnas(t);
        iniciarVer(t);

        if (t.porPagina > 0) {
            t.paginador = document.createElement('div');
            t.paginador.className = 'paginador-tabla';
            tabla.parentNode.insertBefore(t.paginador, tabla.nextSibling);
        }

        aplicar(t);
    }

    function valoresColumna(t, columna) {
        return t.datos.map(function (d) { return textoCelda(d.fila, columna); });
    }

    function compararFilas(t) {
        var columna = t.columna;
        if (!t.tipos[columna]) t.tipos[columna] = tipoDeColumna(valoresColumna(t, columna));
        var tipo = t.tipos[columna];
        var leer = tipo === 'fecha' ? leerFecha : tipo === 'numero' ? leerNumero : null;

        return function (a, b) {
            var va = textoCelda(a.fila, columna), vb = textoCelda(b.fila, columna);
            var resultado;
            if (leer) {
                var na = va ? leer(va) : null, nb = vb ? leer(vb) : null;
                // Las celdas vacías van siempre al final, en los dos sentidos.
                if (na === null && nb === null) resultado = 0;
                else if (na === null) return 1;
                else if (nb === null) return -1;
                else resultado = na - nb;
            } else {
                if (!va && vb) return 1;
                if (va && !vb) return -1;
                resultado = comparador.compare(va, vb);
            }
            return resultado * t.direccion || a.indice - b.indice;
        };
    }

    // Una fila pasa las opciones si en cada grupo coincide con alguno de los valores elegidos.
    function cumpleOpciones(t, d) {
        for (var i = 0; i < t.grupos.length; i++) {
            var grupo = t.grupos[i];
            var elegido = t.opciones[grupo.clave];
            if (!elegido) continue;
            var aceptados = elegido.split('|').map(normalizar);
            if (aceptados.indexOf(d.opciones[grupo.clave]) < 0) return false;
        }
        return true;
    }

    function aplicar(t) {
        var palabras = partirEnPalabras(t.filtro);

        var ordenados = t.datos.slice();
        if (t.columna >= 0 && t.columna < t.encabezado.cells.length) ordenados.sort(compararFilas(t));

        var visibles = ordenados.filter(function (d) {
            return (!palabras.length || coincide(palabras, d.texto, d.compacto)) && cumpleOpciones(t, d);
        });

        var paginas = t.porPagina > 0 ? Math.max(1, Math.ceil(visibles.length / t.porPagina)) : 1;
        if (t.pagina >= paginas) t.pagina = paginas - 1;
        if (t.pagina < 0) t.pagina = 0;
        var desde = t.porPagina > 0 ? t.pagina * t.porPagina : 0;
        var hasta = t.porPagina > 0 ? desde + t.porPagina : visibles.length;

        var mostrar = {};
        visibles.slice(desde, hasta).forEach(function (d) { mostrar[d.indice] = true; });

        // Reordena las filas en el DOM y alterna el color solo entre las que quedan a la vista.
        var contenedor = t.encabezado.parentNode;
        var alterna = false;
        ordenados.forEach(function (d) {
            contenedor.appendChild(d.fila);
            var visible = !!mostrar[d.indice];
            d.fila.style.display = visible ? '' : 'none';
            d.fila.classList.toggle('fila-alterna', visible && alterna);
            if (visible) alterna = !alterna;
        });

        mostrarFilaVacia(t, visibles.length === 0);

        Array.prototype.forEach.call(t.encabezado.cells, function (th, columna) {
            th.classList.toggle('orden-asc', columna === t.columna && t.direccion === 1);
            th.classList.toggle('orden-desc', columna === t.columna && t.direccion === -1);
            if (th.classList.contains('ordenable'))
                th.setAttribute('aria-sort', columna !== t.columna ? 'none' : t.direccion === 1 ? 'ascending' : 'descending');
        });

        if (t.paginador) dibujarPaginador(t, paginas);

        if (t.contador) {
            t.contador.textContent = visibles.length === t.datos.length
                ? (t.datos.length === 1 ? '1 registro' : t.datos.length + ' registros')
                : visibles.length + ' de ' + t.datos.length;
        }

        guardarEstado(t.clave, { f: t.filtro, c: t.columna, d: t.direccion, p: t.pagina, o: t.opciones });
    }

    function mostrarFilaVacia(t, mostrar) {
        if (!t.filaVacia) {
            t.filaVacia = document.createElement('tr');
            t.filaVacia.className = 'fila-sin-resultados';
            var td = document.createElement('td');
            td.colSpan = t.encabezado.cells.length;
            td.textContent = 'No hay filas que coincidan con el filtro.';
            t.filaVacia.appendChild(td);
        }
        if (mostrar) t.encabezado.parentNode.appendChild(t.filaVacia);
        else if (t.filaVacia.parentNode) t.filaVacia.parentNode.removeChild(t.filaVacia);
    }

    function dibujarPaginador(t, paginas) {
        t.paginador.innerHTML = '';
        t.paginador.hidden = paginas <= 1;
        if (paginas <= 1) return;

        var boton = function (texto, destino, habilitado) {
            var b = document.createElement('button');
            b.type = 'button';
            b.className = 'paginador-boton';
            b.textContent = texto;
            b.disabled = !habilitado;
            b.addEventListener('click', function () {
                t.pagina = destino;
                aplicar(t);
            });
            return b;
        };

        var texto = document.createElement('span');
        texto.className = 'paginador-texto';
        texto.textContent = 'Página ' + (t.pagina + 1) + ' de ' + paginas;

        t.paginador.appendChild(boton('◄ Anterior', t.pagina - 1, t.pagina > 0));
        t.paginador.appendChild(texto);
        t.paginador.appendChild(boton('Siguiente ►', t.pagina + 1, t.pagina < paginas - 1));
    }

    // --- Opciones de filtro (botones agrupados debajo de la barra de herramientas) -----------

    function indiceDeColumna(t, nombre) {
        var buscado = normalizar(nombre);
        for (var i = 0; i < t.encabezado.cells.length; i++)
            if (normalizar(t.encabezado.cells[i].textContent) === buscado) return i;
        return -1;
    }

    function iniciarOpciones(t, guardado) {
        var idOpciones = t.tabla.getAttribute('data-opciones-tabla');
        var contenedor = idOpciones ? document.getElementById(idOpciones) : null;
        if (!contenedor) return;

        Array.prototype.forEach.call(contenedor.querySelectorAll('.grupo-opciones'), function (elemento) {
            var columna = elemento.getAttribute('data-columna');
            var atributo = elemento.getAttribute('data-atributo');
            var clave = columna ? 'col:' + normalizar(columna) : 'atr:' + atributo;
            var indice = columna ? indiceDeColumna(t, columna) : -1;
            if (columna && indice < 0) return;

            t.datos.forEach(function (d) {
                d.opciones[clave] = normalizar(columna ? textoCelda(d.fila, indice) : d.fila.getAttribute('data-' + atributo));
            });

            var botones = elemento.querySelectorAll('.opcion');
            var grupo = { clave: clave, botones: botones };
            t.grupos.push(grupo);

            var inicial = guardado && guardado.o && guardado.o[clave] !== undefined
                ? guardado.o[clave]
                : (elemento.getAttribute('data-inicial') || '');
            elegirOpcion(t, grupo, inicial);

            Array.prototype.forEach.call(botones, function (boton) {
                boton.setAttribute('type', 'button');
                boton.addEventListener('click', function () {
                    elegirOpcion(t, grupo, boton.getAttribute('data-valor') || '');
                    t.pagina = 0;
                    aplicar(t);
                });
            });
        });

        t.contador = document.createElement('span');
        t.contador.className = 'contador-tabla';
        t.contador.setAttribute('aria-live', 'polite');
        contenedor.appendChild(t.contador);
    }

    function elegirOpcion(t, grupo, valor) {
        var encontrado = false;
        Array.prototype.forEach.call(grupo.botones, function (boton) {
            var activo = (boton.getAttribute('data-valor') || '') === valor;
            if (activo) encontrado = true;
            boton.classList.toggle('activa', activo);
            boton.setAttribute('aria-pressed', activo ? 'true' : 'false');
        });
        // Un valor guardado que ya no existe vuelve a "todos".
        if (!encontrado && valor) return elegirOpcion(t, grupo, '');
        t.opciones[grupo.clave] = valor;
    }

    // --- Elegir columnas ---------------------------------------------------------------------

    function claveColumnas(t) {
        return 'columnas.' + window.location.pathname.toLowerCase() + '.' + t.clave;
    }

    function aplicarColumnas(t, ocultas) {
        var nombres = Array.prototype.map.call(t.encabezado.cells, function (th) { return normalizar(th.textContent); });
        Array.prototype.forEach.call(t.tabla.rows, function (fila) {
            // La fila de "sin datos" ocupa todo el ancho con una sola celda.
            if (fila.cells.length !== nombres.length) return;
            for (var i = 0; i < nombres.length; i++)
                fila.cells[i].classList.toggle('columna-oculta', ocultas.indexOf(nombres[i]) >= 0);
        });
    }

    function iniciarColumnas(t) {
        var columnas = [];
        Array.prototype.forEach.call(t.encabezado.cells, function (th) {
            if (!esColumnaDeAcciones(th))
                columnas.push({ nombre: normalizar(th.textContent), texto: th.textContent.trim(), oculta: th.classList.contains('oculta') });
        });
        if (columnas.length < 3) return;

        var ocultas = leerPreferencia(claveColumnas(t));
        if (!Array.isArray(ocultas))
            ocultas = columnas.filter(function (c) { return c.oculta; }).map(function (c) { return c.nombre; });
        aplicarColumnas(t, ocultas);

        var selector = document.createElement('div');
        selector.className = 'selector-columnas';
        var boton = document.createElement('button');
        boton.type = 'button';
        boton.className = 'boton-columnas';
        boton.setAttribute('aria-expanded', 'false');
        boton.innerHTML = '<svg viewBox="0 0 16 16" aria-hidden="true"><rect x="1.5" y="2.5" width="13" height="11" rx="1.5" fill="none" stroke="currentColor" stroke-width="1.4"/><path d="M6 2.5v11M10 2.5v11" stroke="currentColor" stroke-width="1.4"/></svg><span>Columnas</span>';
        var panel = document.createElement('div');
        panel.className = 'panel-columnas';
        panel.hidden = true;

        var titulo = document.createElement('div');
        titulo.className = 'panel-columnas-titulo';
        titulo.textContent = 'Columnas visibles';
        panel.appendChild(titulo);

        columnas.forEach(function (columna, indice) {
            var etiqueta = document.createElement('label');
            var casilla = document.createElement('input');
            casilla.type = 'checkbox';
            casilla.checked = ocultas.indexOf(columna.nombre) < 0;
            casilla.id = t.clave + '_columna' + indice;
            casilla.addEventListener('change', function () {
                var visibles = panel.querySelectorAll('input:checked').length;
                if (!casilla.checked && visibles === 0) {
                    casilla.checked = true; // siempre queda al menos una columna
                    return;
                }
                if (casilla.checked) ocultas = ocultas.filter(function (n) { return n !== columna.nombre; });
                else if (ocultas.indexOf(columna.nombre) < 0) ocultas.push(columna.nombre);
                aplicarColumnas(t, ocultas);
                guardarPreferencia(claveColumnas(t), ocultas);
            });
            etiqueta.appendChild(casilla);
            etiqueta.appendChild(document.createTextNode(' ' + columna.texto));
            panel.appendChild(etiqueta);
        });

        var restaurar = document.createElement('button');
        restaurar.type = 'button';
        restaurar.className = 'panel-columnas-restaurar';
        restaurar.textContent = 'Volver a las columnas de siempre';
        restaurar.addEventListener('click', function () {
            ocultas = columnas.filter(function (c) { return c.oculta; }).map(function (c) { return c.nombre; });
            Array.prototype.forEach.call(panel.querySelectorAll('input'), function (casilla, i) {
                casilla.checked = ocultas.indexOf(columnas[i].nombre) < 0;
            });
            aplicarColumnas(t, ocultas);
            guardarPreferencia(claveColumnas(t), null);
        });
        panel.appendChild(restaurar);

        selector.appendChild(boton);
        selector.appendChild(panel);

        var cerrar = function () {
            panel.hidden = true;
            boton.setAttribute('aria-expanded', 'false');
        };
        boton.addEventListener('click', function () {
            panel.hidden = !panel.hidden;
            boton.setAttribute('aria-expanded', panel.hidden ? 'false' : 'true');
        });
        document.addEventListener('click', function (e) {
            if (!panel.hidden && !selector.contains(e.target)) cerrar();
        });
        selector.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !panel.hidden) {
                e.stopPropagation();
                cerrar();
                boton.focus();
            }
        });

        // En la barra de herramientas de la pantalla (antes del botón "Nuevo …"), o al lado del
        // filtro que se agregó arriba de la tabla.
        var barra = t.input ? t.input.closest('.barra-herramientas, .filtro-tabla') : null;
        if (barra) {
            var acciones = barra.querySelector('.acciones-barra');
            if (acciones) barra.insertBefore(selector, acciones);
            else barra.appendChild(selector);
        } else {
            var contenedor = document.createElement('div');
            contenedor.className = 'filtro-tabla';
            contenedor.appendChild(selector);
            t.tabla.parentNode.insertBefore(contenedor, t.tabla);
        }
    }

    // --- Ver todos los datos de una fila -------------------------------------------------------

    var modalDetalle = null;

    function crearModalDetalle(pantalla) {
        var modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'modalDetalleFila';
        modal.tabIndex = -1;
        modal.setAttribute('aria-hidden', 'true');
        modal.setAttribute('aria-labelledby', 'tituloDetalleFila');
        modal.innerHTML =
            '<div class="modal-dialog modal-lg modal-dialog-scrollable"><div class="modal-content">' +
            '<div class="modal-header"><h2 class="modal-title" id="tituloDetalleFila"></h2>' +
            '<button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button></div>' +
            '<div class="modal-body"><div class="imagen-detalle" hidden><img alt=""></div>' +
            '<dl class="datos-resumen datos-detalle"></dl></div>' +
            '<div class="modal-footer"><span class="acciones-secundarias"></span>' +
            '<button type="button" class="boton-gris" data-bs-dismiss="modal">Cerrar</button></div>' +
            '</div></div>';
        pantalla.appendChild(modal);
        return modal;
    }

    function verFila(t, fila) {
        var pantalla = t.tabla.closest('.pantalla-abm') || document.body;
        if (!modalDetalle || !pantalla.contains(modalDetalle)) modalDetalle = crearModalDetalle(pantalla);

        var celdaTitulo = fila.querySelector('.celda-titulo') || fila.cells[0];
        var prefijo = t.tabla.getAttribute('data-titulo-detalle');
        var titulo = celdaTitulo ? celdaTitulo.textContent.replace(/\s+/g, ' ').trim() : '';
        modalDetalle.querySelector('.modal-title').textContent = prefijo ? prefijo + ': ' + titulo : titulo;

        // La imagen de la fila, si tiene: la versión grande, que la miniatura trae en data-imagen.
        var miniatura = fila.querySelector('img[data-imagen]');
        var marco = modalDetalle.querySelector('.imagen-detalle');
        var imagen = marco.querySelector('img');
        marco.hidden = !miniatura;
        if (miniatura) {
            imagen.src = miniatura.getAttribute('data-imagen');
            imagen.alt = titulo;
        } else {
            imagen.removeAttribute('src');
        }

        var lista = modalDetalle.querySelector('dl');
        lista.innerHTML = '';
        Array.prototype.forEach.call(t.encabezado.cells, function (th, i) {
            if (esColumnaDeAcciones(th)) return;
            var dt = document.createElement('dt');
            dt.textContent = th.textContent.trim();
            var dd = document.createElement('dd');
            dd.textContent = textoCelda(fila, i) || '—';
            lista.appendChild(dt);
            lista.appendChild(dd);
        });

        // Las acciones de la fila (Editar, Cambiar dueño…) también desde la ventana.
        var acciones = modalDetalle.querySelector('.acciones-secundarias');
        acciones.innerHTML = '';
        Array.prototype.forEach.call(fila.querySelectorAll('.accion-detalle'), function (accion) {
            var boton = document.createElement('button');
            boton.type = 'button';
            boton.className = 'boton-rojo';
            boton.textContent = accion.textContent.trim();
            boton.addEventListener('click', function () {
                bootstrap.Modal.getOrCreateInstance(modalDetalle).hide();
                accion.click();
            });
            acciones.appendChild(boton);
        });

        if (window.bootstrap) bootstrap.Modal.getOrCreateInstance(modalDetalle).show();
    }

    function iniciarVer(t) {
        t.datos.forEach(function (d) {
            var ver = d.fila.querySelector('.accion-ver');

            if (ver && ver.hasAttribute('data-ver-detalle')) {
                ver.addEventListener('click', function (e) {
                    e.preventDefault();
                    verFila(t, d.fila);
                });
            }

            // La celda del nombre (o del número) abre lo mismo que "Ver".
            if (!ver) return;
            Array.prototype.forEach.call(d.fila.querySelectorAll('.celda-titulo'), function (celda) {
                if (celda.querySelector('a')) return;
                var enlace = document.createElement('a');
                enlace.href = '#';
                enlace.className = 'enlace-titulo';
                while (celda.firstChild) enlace.appendChild(celda.firstChild);
                celda.appendChild(enlace);
                enlace.addEventListener('click', function (e) {
                    e.preventDefault();
                    ver.click();
                });
            });
        });
    }

    // data-estatica: tabla con el estilo de las demás pero sin orden ni filtro (la matriz de
    // permisos de Roles, que tiene filas de grupo que no se pueden mover).
    function iniciarTablas(raiz) {
        var tablas = raiz.querySelectorAll('table.tabla-abm:not([data-estatica])');
        for (var i = 0; i < tablas.length; i++)
            if (!tablas[i].hasAttribute('data-tabla-iniciada')) iniciarTabla(tablas[i]);
    }

    // --- Selectores con búsqueda ---------------------------------------------------------

    var MaximoOpcionesVisibles = 100;

    function iniciarSelector(contenedor) {
        contenedor.setAttribute('data-selector-iniciado', '');

        var texto = contenedor.querySelector('.selector-texto');
        var oculto = contenedor.querySelector('input[type="hidden"]');
        var boton = contenedor.querySelector('.selector-boton');
        if (!texto || !oculto) return;

        var opciones;
        try {
            opciones = JSON.parse(contenedor.getAttribute('data-opciones') || '[]');
        } catch (e) {
            opciones = [];
        }
        opciones.forEach(function (o) {
            o.normalizado = normalizar(o.t);
            o.compacto = compactar(o.normalizado);
        });

        var deshabilitado = texto.disabled || texto.readOnly;
        if (boton) boton.disabled = deshabilitado;
        if (deshabilitado) return;

        var lista = document.createElement('ul');
        lista.className = 'selector-lista';
        lista.setAttribute('role', 'listbox');
        lista.hidden = true;
        contenedor.appendChild(lista);
        texto.setAttribute('role', 'combobox');
        texto.setAttribute('aria-autocomplete', 'list');
        texto.setAttribute('aria-expanded', 'false');

        // El campo muestra siempre el texto de la opción elegida: si el usuario escribió algo y
        // mandó el formulario sin elegir, al volver del servidor se corrige acá. Si lo elegido
        // no está entre las opciones (ej. un dueño ya dado de baja), queda el texto del servidor.
        for (var i = 0; i < opciones.length; i++) {
            if (opciones[i].v === oculto.value) {
                texto.value = opciones[i].t;
                break;
            }
        }
        if (!oculto.value) texto.value = '';

        // Si el usuario escribe y se va sin elegir nada, el campo vuelve a mostrar lo elegido.
        var textoElegido = texto.value;
        var visibles = [];
        var activa = -1;

        function dibujar(filtro) {
            var palabras = partirEnPalabras(filtro);
            visibles = opciones.filter(function (o) {
                return !palabras.length || coincide(palabras, o.normalizado, o.compacto);
            });
            lista.innerHTML = '';
            activa = -1;

            if (!visibles.length) {
                var vacio = document.createElement('li');
                vacio.className = 'selector-vacio';
                vacio.textContent = opciones.length ? 'No hay coincidencias.' : 'No hay opciones cargadas.';
                lista.appendChild(vacio);
                return;
            }

            visibles.slice(0, MaximoOpcionesVisibles).forEach(function (o, indice) {
                var li = document.createElement('li');
                li.className = 'selector-opcion' + (o.v === oculto.value ? ' elegida' : '');
                li.setAttribute('role', 'option');
                li.textContent = o.t;
                li.addEventListener('click', function () { elegir(o); });
                li.addEventListener('mousemove', function () { marcar(indice); });
                lista.appendChild(li);
            });

            if (visibles.length > MaximoOpcionesVisibles) {
                var mas = document.createElement('li');
                mas.className = 'selector-vacio';
                mas.textContent = 'Hay más resultados: escribí para acotar la búsqueda.';
                lista.appendChild(mas);
            }
        }

        function marcar(indice) {
            var items = lista.querySelectorAll('.selector-opcion');
            if (!items.length) return;
            if (indice < 0) indice = items.length - 1;
            if (indice >= items.length) indice = 0;
            if (activa >= 0 && items[activa]) items[activa].classList.remove('activa');
            activa = indice;
            items[activa].classList.add('activa');
            items[activa].scrollIntoView({ block: 'nearest' });
        }

        function abrir(filtro) {
            dibujar(filtro);
            lista.hidden = false;
            contenedor.classList.add('abierto');
            texto.setAttribute('aria-expanded', 'true');
        }

        function cerrar() {
            lista.hidden = true;
            contenedor.classList.remove('abierto');
            texto.setAttribute('aria-expanded', 'false');
            activa = -1;
        }

        function elegir(opcion) {
            var cambio = oculto.value !== opcion.v;
            oculto.value = opcion.v;
            texto.value = opcion.t;
            textoElegido = opcion.t;
            cerrar();
            if (cambio && contenedor.getAttribute('data-postback') === 'true') {
                if (typeof window.__doPostBack === 'function') window.__doPostBack(oculto.name, '');
                else if (oculto.form) oculto.form.submit();
            }
        }

        // El texto mostrado coincide con lo elegido: abrir muestra todas las opciones; si el
        // usuario ya escribió algo, muestra solo las que coinciden.
        function filtroActual() {
            return texto.value === textoElegido ? '' : texto.value;
        }

        texto.addEventListener('click', function () {
            if (lista.hidden) abrir(filtroActual());
        });

        texto.addEventListener('input', function () {
            abrir(texto.value);
        });

        texto.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                e.preventDefault();
                if (lista.hidden) abrir(filtroActual());
                marcar(activa + (e.key === 'ArrowDown' ? 1 : -1));
            } else if (e.key === 'Enter') {
                // Nunca manda el formulario: Enter elige la opción marcada (o la única que quedó).
                e.preventDefault();
                if (!lista.hidden && activa >= 0 && visibles[activa]) elegir(visibles[activa]);
                else if (!lista.hidden && visibles.length === 1) elegir(visibles[0]);
            } else if (e.key === 'Escape') {
                if (!lista.hidden) {
                    e.preventDefault();
                    e.stopPropagation();
                    cerrar();
                    texto.value = textoElegido;
                }
            }
        });

        texto.addEventListener('blur', function () {
            setTimeout(function () {
                if (contenedor.contains(document.activeElement)) return;
                cerrar();
                if (texto.value !== textoElegido) texto.value = textoElegido;
            }, 150);
        });

        if (boton) {
            boton.addEventListener('click', function () {
                if (lista.hidden) {
                    texto.focus();
                    abrir(filtroActual());
                } else {
                    cerrar();
                }
            });
        }

        // Que hacer clic en la lista no le saque el foco al campo (si no, se cerraría antes
        // de registrar el clic en la opción).
        lista.addEventListener('mousedown', function (e) { e.preventDefault(); });
    }

    function iniciarSelectores(raiz) {
        var selectores = raiz.querySelectorAll('.selector-busqueda');
        for (var i = 0; i < selectores.length; i++)
            if (!selectores[i].hasAttribute('data-selector-iniciado')) iniciarSelector(selectores[i]);
    }

    // --- Modales -----------------------------------------------------------------------------

    Lubricentro.abrirModal = function (id) {
        var abrir = function () {
            var elemento = document.getElementById(id);
            if (!elemento || !window.bootstrap) return;

            // Después de un postback parcial (UpdatePanel adentro del modal) ya está abierto.
            if (elemento.classList.contains('show')) return;

            // Sin la animación de entrada: después de un postback el formulario tiene que
            // verse como si nunca se hubiera cerrado. Al cerrarlo sí se anima.
            elemento.classList.remove('fade');
            elemento.addEventListener('shown.bs.modal', function () {
                elemento.classList.add('fade');
            }, { once: true });
            bootstrap.Modal.getOrCreateInstance(elemento).show();
        };

        // El servidor lo llama al final del formulario, antes de que cargue bootstrap.bundle.js.
        if (document.readyState === 'loading' || !window.bootstrap)
            document.addEventListener('DOMContentLoaded', abrir);
        else
            abrir();
    };

    // --- Campos que dependen de otra elección ---------------------------------------------
    // data-mostrar-si="idControl=Valor" (o "Valor1|Valor2") muestra el elemento solo mientras el
    // desplegable o el grupo de opciones idControl tenga ese valor: los datos de una empresa o de
    // una persona, los de stock de un insumo, la alícuota de un producto gravado. El servidor
    // valida igual cada caso; esto solo esconde lo que no corresponde completar.

    function valorDe(control) {
        if (control.tagName === 'SELECT' || control.tagName === 'INPUT') return control.value;
        var elegido = control.querySelector('input:checked');
        return elegido ? elegido.value : '';
    }

    function iniciarCondicionales(raiz) {
        Array.prototype.forEach.call(raiz.querySelectorAll('[data-mostrar-si]'), function (elemento) {
            if (elemento.hasAttribute('data-condicional-iniciado')) return;
            elemento.setAttribute('data-condicional-iniciado', '');

            var regla = elemento.getAttribute('data-mostrar-si');
            var corte = regla.indexOf('=');
            var control = document.getElementById(regla.substring(0, corte));
            if (!control) return;
            var valores = regla.substring(corte + 1).split('|');

            var actualizar = function () {
                elemento.hidden = valores.indexOf(valorDe(control)) < 0;
            };
            control.addEventListener('change', actualizar);
            actualizar();
        });
    }

    // --- Campo de imagen (.campo-imagen) -------------------------------------------------------
    // El recuadro (.campo-imagen-vista img) muestra la imagen elegida antes de guardarla. Un
    // archivo que no es PNG ni JPG, o que pesa más que data-tamano-maximo (bytes, en el campo de
    // archivo), se descarta con un aviso sin mandarlo: el servidor lo rechazaría igual, pero
    // después de esperar la subida. "Quitar la imagen" deja el recuadro vacío.

    function iniciarCampoImagen(campo) {
        campo.setAttribute('data-campo-iniciado', '');

        var archivo = campo.querySelector('input[type="file"]');
        var vista = campo.querySelector('.campo-imagen-vista img');
        var aviso = campo.querySelector('.campo-imagen-error');
        var quitar = campo.querySelector('.campo-imagen-quitar input[type="checkbox"]');
        if (!archivo || !vista) return;

        var guardada = vista.getAttribute('src');
        var maximo = parseInt(archivo.getAttribute('data-tamano-maximo'), 10) || 0;
        var elegida = null;

        var mostrar = function (src) {
            // La vista previa de un archivo anterior ya no se usa: se libera.
            if (elegida && elegida !== src) {
                URL.revokeObjectURL(elegida);
                elegida = null;
            }
            if (src) vista.src = src;
            else vista.removeAttribute('src');
            vista.hidden = !src;
        };

        // Sin archivo nuevo, el recuadro vuelve a la imagen guardada (vacío si se va a quitar).
        var mostrarGuardada = function () {
            mostrar(quitar && quitar.checked ? null : guardada);
        };

        var avisar = function (texto) {
            if (!aviso) return;
            aviso.textContent = texto;
            aviso.hidden = !texto;
        };

        archivo.addEventListener('change', function () {
            var elegido = archivo.files && archivo.files[0];
            avisar('');
            if (!elegido) {
                mostrarGuardada();
                return;
            }

            if (elegido.type !== 'image/png' && elegido.type !== 'image/jpeg') {
                avisar('La imagen tiene que ser PNG o JPG.');
            } else if (maximo && elegido.size > maximo) {
                avisar('La imagen no puede pesar más de ' + Math.round(maximo / 1048576) + ' MB.');
            } else {
                if (quitar) quitar.checked = false;
                var url = URL.createObjectURL(elegido);
                mostrar(url);
                elegida = url;
                return;
            }
            archivo.value = '';
            mostrarGuardada();
        });

        if (quitar) {
            quitar.addEventListener('change', function () {
                if (quitar.checked) {
                    archivo.value = '';
                    avisar('');
                }
                mostrarGuardada();
            });
        }
    }

    function iniciarCamposImagen(raiz) {
        var campos = raiz.querySelectorAll('.campo-imagen');
        for (var i = 0; i < campos.length; i++)
            if (!campos[i].hasAttribute('data-campo-iniciado')) iniciarCampoImagen(campos[i]);
    }

    // Roles: pone todas las pantallas de la matriz en el mismo acceso (las fijas, como Inicio,
    // vienen deshabilitadas y no se tocan).
    Lubricentro.marcarPermisos = function (acceso) {
        Array.prototype.forEach.call(document.querySelectorAll('.acceso-pantalla input[type="radio"]'), function (radio) {
            if (!radio.disabled && radio.value === acceso) radio.checked = true;
        });
    };

    // Muestra u oculta un bloque de la pantalla (ej. la confirmación de cierre de una orden).
    Lubricentro.alternar = function (id) {
        var elemento = document.getElementById(id);
        if (!elemento) return;
        elemento.hidden = !elemento.hidden;
        if (!elemento.hidden) elemento.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    };

    // --- Barra lateral ---------------------------------------------------------------------

    var ClaveMenu = 'menuAbiertos';

    function gruposDelMenu() {
        return document.querySelectorAll('.menu-lateral .collapse[data-grupo]');
    }

    // Se llama desde el Site.Master apenas se dibuja el menú, antes de que se vea: reabre los
    // grupos que el usuario dejó abiertos (el de la pantalla actual ya viene abierto) y desde
    // ahí recuerda cada grupo que se abre o se cierra.
    Lubricentro.restaurarMenu = function () {
        var abiertos = leerPreferencia(ClaveMenu) || [];

        Array.prototype.forEach.call(gruposDelMenu(), function (grupo) {
            var boton = document.querySelector('[data-bs-target="#' + grupo.id + '"]');
            if (abiertos.indexOf(grupo.getAttribute('data-grupo')) >= 0 && !grupo.classList.contains('show')) {
                grupo.classList.add('show');
                if (boton) {
                    boton.classList.remove('collapsed');
                    boton.setAttribute('aria-expanded', 'true');
                }
            }

            var recordar = function () {
                var lista = [];
                Array.prototype.forEach.call(gruposDelMenu(), function (g) {
                    if (g.classList.contains('show')) lista.push(g.getAttribute('data-grupo'));
                });
                guardarPreferencia(ClaveMenu, lista);
            };
            grupo.addEventListener('shown.bs.collapse', recordar);
            grupo.addEventListener('hidden.bs.collapse', recordar);
        });
    };

    // --- Arranque ----------------------------------------------------------------------------

    function iniciar() {
        iniciarTablas(document);
        iniciarSelectores(document);
        iniciarCondicionales(document);
        iniciarCamposImagen(document);
    }

    // Sys.Application.add_load corre al cargar la página y otra vez después de cada postback
    // parcial de un UpdatePanel (que reemplaza su contenido por elementos nuevos, sin iniciar).
    if (window.Sys && Sys.Application) Sys.Application.add_load(iniciar);
    else document.addEventListener('DOMContentLoaded', iniciar);

    Lubricentro.iniciar = iniciar;
})();
