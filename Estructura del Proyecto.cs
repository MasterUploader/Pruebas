function etiquetarProveedoresExternos() {
  const etiquetaNombre = "Proveedores Externos";
  const etiqueta = GmailApp.getUserLabelByName(etiquetaNombre) || GmailApp.createLabel(etiquetaNombre);

  const dominios = [
    "@andes.la",
    "@solutechn.com",
    "@bancajerosbanet.com",
    "@csicompany.com",
    "@its-hn.net",
    "@evertecinc.com",
    "@gbm.net",
    "@transunion.com",
    "@cbns.gob.hn",
    "@sfin.gob.hn",
    "@rocketmail.com",
    "@aseinfocom.sv",
    "@tcs.com",
    "@technisys.com",
    "@ticnow.cl",
    "@dxc.com",
    "@nrp.hn",
    "@agrega.hn",
    "@davinciotech.com",
    "@swift.com",
    "@google.com",
    "@networkhn.com",
    "@claro.com.hn",
    "@sac.tigo.com.hn",
    "@movizzon.com",
    "@ceproban.hn",
    "@paysett.com",
    "@greenlt.com",
    "@twco.com",
    "@palig.com",
    "@stigobals.com",
    "@ginh.com",
    "@proveedores.davivienda.cr",
    "@is4tech.com"
  ];

  // Buscar hilos nuevos no leídos, sin etiquetar aún
  const threads = GmailApp.search('is:unread -label:"' + etiquetaNombre + '"');

  for (const thread of threads) {
    const messages = thread.getMessages();
    for (const msg of messages) {
      const from = msg.getFrom().toLowerCase();

      // Verifica si el remitente coincide con alguno de los dominios
      if (dominios.some(d => from.includes(d))) {
        etiqueta.addToThread(thread);
        break; // Etiqueta una sola vez por hilo
      }
    }
  }
}
