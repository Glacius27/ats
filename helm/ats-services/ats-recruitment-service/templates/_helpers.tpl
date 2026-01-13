{{- define "ats-recruitment-service.name" -}}
ats-recruitment-service
{{- end }}

{{- define "ats-recruitment-service.fullname" -}}
{{ printf "%s" (include "ats-recruitment-service.name" .) }}
{{- end }}
