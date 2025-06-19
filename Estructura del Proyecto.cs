function etiquetarInvitacionesGoogleMeet() {
  const etiquetaNombre = "Invitaciones Meet";
  const etiqueta = GmailApp.getUserLabelByName(etiquetaNombre) || GmailApp.createLabel(etiquetaNombre);

  // Buscar correos no leídos que contienen enlaces a Google Meet y aún no tienen la etiqueta
  const threads = GmailApp.search('is:unread -label:"' + etiquetaNombre + '" ("meet.google.com" OR "invite.google.com")');

  for (const thread of threads) {
    etiqueta.addToThread(thread);
    // Nota: NO se archiva, para mantenerlo visible en "Recibidos"
  }
}
