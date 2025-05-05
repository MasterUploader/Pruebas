// ===========================
        //   2. MANTENIMIENTO VIDEOS
        // ===========================

        [HttpGet]
        public IActionResult Index(string codcco)
        {
            if (string.IsNullOrEmpty(codcco))
            {
                ViewBag.Mensaje = "Debe proporcionar un código de agencia.";
                return View(new List<VideoModel>());
            }

            var lista = _videoService.ListarVideos(codcco);
            return View(lista);
        }

        [HttpPost]
        public IActionResult Actualizar(int codVideo, string codcco, string Estado, int Seq)
        {
            var video = new VideoModel
            {
                CodVideo = codVideo,
                Codcco = codcco,
                Estado = Estado,
                Seq = Seq
            };

            var actualizado = _videoService.ActualizarVideo(video);

            ViewBag.Mensaje = actualizado
                ? "Registro actualizado correctamente."
                : "Error al actualizar el registro.";

            return RedirectToAction("Index", new { codcco = codcco });
        }

        [HttpPost]
        public IActionResult Eliminar(int codVideo, string codcco)
        {
            // Validar dependencias
            if (_videoService.TieneDependencias(codcco, codVideo))
            {
                ViewBag.Mensaje = "No se puede eliminar el video porque tiene dependencias.";
                return RedirectToAction("Index", new { codcco = codcco });
            }

            var lista = _videoService.ListarVideos(codcco);
            var video = lista.FirstOrDefault(v => v.CodVideo == codVideo);

            if (video == null)
            {
                ViewBag.Mensaje = "El video no fue encontrado.";
                return RedirectToAction("Index", new { codcco = codcco });
            }

            var eliminadoDb = _videoService.EliminarVideo(codVideo, codcco);
            var eliminadoArchivo = _videoService.EliminarArchivoFisico(video.RutaFisica);

            ViewBag.Mensaje = eliminadoDb && eliminadoArchivo
                ? "Video eliminado correctamente."
                : "Error al eliminar el video.";

            return RedirectToAction("Index", new { codcco = codcco });
        }
    }
