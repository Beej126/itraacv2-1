<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:variable name="IsNumeric" select="'0123456789,.'"/>

  <xsl:template match="/">
    <div >
      Records:
      <xsl:value-of select="count(NewDataSet/*)"/>
    </div>
    <table border="1" style="border-collapse:collapse;" cellpadding="3">
      <xsl:for-each select="/NewDataSet/*" >
        <xsl:if test="local-name(preceding-sibling::*[1])!=local-name()" >
          <xsl:text disable-output-escaping="yes"><![CDATA[</table><br/><table border="1" style="border-collapse:collapse;" cellpadding="3">]]></xsl:text>
          <tr class="header" bgcolor="#9acd32" >
            <xsl:for-each select="./*">
              <th>
                <xsl:value-of select="local-name()" />
              </th>
            </xsl:for-each>
          </tr>
        </xsl:if>
        <tr>
          <xsl:for-each select="./*">
            <td>
              <xsl:if test="string-length(translate(., $IsNumeric, '')) = 0">
                <xsl:attribute name="align">right</xsl:attribute>
              </xsl:if>
              <xsl:value-of select="." />
            </td>
          </xsl:for-each>
        </tr>
      </xsl:for-each>
    </table>
    <div style="margin-top: 10px">(end of results)</div>
  </xsl:template>
</xsl:stylesheet>

